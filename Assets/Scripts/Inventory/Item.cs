using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Item : MonoBehaviour, IInteractable
{
    [Header("Inventory")]
    public string itemName;
    public Item itemPrefab;
    public int amount = 1;

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

    private void Awake()
    {
        anim = GetComponent<Animator>();
        if (promptObject != null)
            promptObject.SetActive(false);

        if (ignorePlayerCollision)
            IgnorePlayerCollision();

        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
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
            rb.gravityScale = -1.0f;
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
