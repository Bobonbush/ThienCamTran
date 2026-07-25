using System;
using UnityEngine;

[Serializable]
public sealed class ItemData
{
    public string id;
    public string itemName;
    public Item.Category category;
    public Sprite icon;
    [TextArea] public string description;
    public int healthRestore;
    public int manaRestore;
    public float staminaRestore;
    public float maxHealthMultiplier = 1f;
    public float maxManaMultiplier = 1f;
    public float maxStaminaMultiplier = 1f;
    public float staminaRegenMultiplier = 1f;
    public float staminaCostMultiplier = 1f;
    public float manaGainMultiplier = 1f;
    public float healingMultiplier = 1f;

    public static ItemData From(Item item)
    {
        if (item == null) return null;
        return new ItemData
        {
            id = item.ItemId,
            itemName = item.ItemName,
            category = item.ItemCategory,
            icon = item.Icon,
            description = item.description,
            healthRestore = item.healthRestore,
            manaRestore = item.manaRestore,
            staminaRestore = item.staminaRestore,
            maxHealthMultiplier = item.maxHealthMultiplier,
            maxManaMultiplier = item.maxManaMultiplier,
            maxStaminaMultiplier = item.maxStaminaMultiplier,
            staminaRegenMultiplier = item.staminaRegenMultiplier,
            staminaCostMultiplier = item.staminaCostMultiplier,
            manaGainMultiplier = item.manaGainMultiplier,
            healingMultiplier = item.healingMultiplier
        };
    }
}

[RequireComponent(typeof(Collider2D))]
public class Item : MonoBehaviour, IInteractable
{
    public enum Category { Unknown, Food, Buff, Lore, Currency }

    [Header("Inventory")]
    public string itemName;
    public Item itemPrefab;
    public int amount = 1;

    [Header("Gameplay data")]
    [SerializeField] private Category category;
    [Min(0)] public int healthRestore;
    [Min(0)] public int manaRestore;
    [Min(0)] public float staminaRestore;
    [Min(0.01f)] public float maxHealthMultiplier = 1f;
    [Min(0.01f)] public float maxManaMultiplier = 1f;
    [Min(0.01f)] public float maxStaminaMultiplier = 1f;
    [Min(0f)] public float staminaRegenMultiplier = 1f;
    [Min(0f)] public float staminaCostMultiplier = 1f;
    [Min(0f)] public float manaGainMultiplier = 1f;
    [Min(0f)] public float healingMultiplier = 1f;

    [SerializeField]
    IInteractable.Type type = IInteractable.Type.Item;

    private float invisibleframe = 0.25f;

    public bool autoPickUp = false;

    [Header("Pickup")]
    public GameObject promptObject;
    public float pickupDelay = 0.25f;
    public bool ignorePlayerCollision = true;

    [Header("Presentation")]
    [Tooltip("Icon cho popup 'Vật Phẩm Mới'; bỏ trống sẽ lấy sprite trên SpriteRenderer")]
    public Sprite icon;
    [TextArea]
    [Tooltip("Cốt truyện/mô tả hiện trong popup khi nhặt vật phẩm này lần đầu")]
    public string description;

    Animator anim;

    Rigidbody2D rb;

    private static PhysicsMaterial2D droppedItemMaterial;
    private bool isRuntimeDrop;
    private Vector2 dropRecoveryPosition;

    private float spawnedAt;

    private bool picked = false;

    public bool CanInteract
    {
        get { return Time.time >= spawnedAt + pickupDelay; }
    }

    public string ItemName
    {
        get { return string.IsNullOrWhiteSpace(itemName) ? gameObject.name.Replace("(Clone)", "").Trim() : itemName; }
    }

    public Item InventoryPrefab
    {
        get { return itemPrefab != null ? itemPrefab : this; }
    }

    public Sprite Icon
    {
        get
        {
            if (icon != null)
                return icon;

            SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
            return sprite != null ? sprite.sprite : null;
        }
    }

    public string ItemId => gameObject.name.Replace("(Clone)", string.Empty).Trim();

    public Category ItemCategory => category;
    private string saveID = string.Empty;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        saveID = SaveIdUtility.For(this);
        Debug.Log("Item with ID : " + saveID);
        if (promptObject != null)
            promptObject.SetActive(false);

        if (ignorePlayerCollision)
            IgnorePlayerCollision();

        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {

        if (ItemCategory == Category.Lore && SaveManager.Instance.IsItemObtained(saveID))
        {
            gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        spawnedAt = Time.time;
    }

    public void SetPromptVisible(bool visible)
    {
        if (promptObject != null && promptObject.activeSelf != visible && picked == false)
            promptObject.SetActive(visible);
    }

    public new IInteractable.Type GetType()
    {
        return type;
    }

    private void Update()
    {
        invisibleframe -= Time.deltaTime;
    }

    public void Interact(PlayerController player)
    {
        if (!CanInteract || player == null || picked == true || invisibleframe > 0.0f)
            return;

        
        Inventory inventory = player.GetComponent<Inventory>();
        if (inventory != null && inventory.AddItem(this, amount))
        {
            picked = true;
            isRuntimeDrop = false;
            rb.gravityScale = -1.0f;
            SaveManager.Instance.MarkItemObtained(saveID);
            Sfx.PlayAt(SfxId.PlayerPickup, transform.position);

            if (anim != null)
            {
                
                anim.SetTrigger("Consume");
            }else
            {
                Destroy(gameObject);
            }
            
        }
    }

    public void AnimationDestroy()
    {
        Destroy(gameObject);
    }

    public void ConfigureDroppedPhysics(Vector2 safePosition)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb == null) return;

        isRuntimeDrop = true;
        dropRecoveryPosition = safePosition;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.simulated = true;
        rb.gravityScale = Mathf.Max(1f, rb.gravityScale);
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        rb.linearDamping = Mathf.Max(0.35f, rb.linearDamping);
        rb.angularDamping = Mathf.Max(0.5f, rb.angularDamping);

        if (droppedItemMaterial == null)
        {
            droppedItemMaterial = new PhysicsMaterial2D("Dropped Item Bounce")
            {
                friction = 0.18f,
                bounciness = 0.48f,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        foreach (Collider2D itemCollider in GetComponentsInChildren<Collider2D>())
        {
            if (!itemCollider.isTrigger)
                itemCollider.sharedMaterial = droppedItemMaterial;
        }
    }

    private void FixedUpdate()
    {
        if (!isRuntimeDrop || picked || rb == null) return;

        Vector2 position = rb.position;
        bool invalidPosition = float.IsNaN(position.x) || float.IsNaN(position.y) ||
                               float.IsInfinity(position.x) || float.IsInfinity(position.y);
        if (!invalidPosition && position.y >= dropRecoveryPosition.y - 6f) return;

        rb.position = dropRecoveryPosition + Vector2.up * 0.35f;
        rb.linearVelocity = Vector2.up * 1.5f;
        rb.angularVelocity = 0f;
        Physics2D.SyncTransforms();
    }

    private void IgnorePlayerCollision()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        Collider2D[] itemColliders = GetComponentsInChildren<Collider2D>();
        Collider2D[] playerColliders = player.GetComponentsInChildren<Collider2D>();

        foreach (Collider2D itemCollider in itemColliders)
            foreach (Collider2D playerCollider in playerColliders)
                Physics2D.IgnoreCollision(itemCollider, playerCollider, true);
    }
}
