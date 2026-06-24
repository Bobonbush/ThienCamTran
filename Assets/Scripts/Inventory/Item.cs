using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Item : MonoBehaviour, IInteractable
{
    [Header("Inventory")]
    public string itemName;
    public Item itemPrefab;
    public int amount = 1;

    [Header("Pickup")]
    public GameObject promptObject;
    public float pickupDelay = 0.25f;
    public bool ignorePlayerCollision = true;

    private float spawnedAt;

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

    private void Awake()
    {
        if (promptObject != null)
            promptObject.SetActive(false);

        if (ignorePlayerCollision)
            IgnorePlayerCollision();
    }

    private void OnEnable()
    {
        spawnedAt = Time.time;
    }

    public void SetPromptVisible(bool visible)
    {
        if (promptObject != null && promptObject.activeSelf != visible)
            promptObject.SetActive(visible);
    }

    public void Interact(PlayerController player)
    {
        if (!CanInteract || player == null)
            return;

        Inventory inventory = player.GetComponent<Inventory>();
        if (inventory != null && inventory.AddItem(this, amount))
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
