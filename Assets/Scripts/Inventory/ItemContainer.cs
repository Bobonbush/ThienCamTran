using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemContainer : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public struct itemInfo
    {
        public Item item;
        public int count;
    };
    [Header("Items")]

    public List<itemInfo> itemPrefabs = new List<itemInfo>();
    public bool openOnce = true;

    [Header("Drop")]
    public Transform dropPoint;
    public float burstForce = 4f;
    public float upwardForce = 3f;
    public float spreadX = 1.5f;
    public float spreadY = 0.5f;

    EnemySpawn e_spawn;
    [Header("Prompt")]
    public GameObject promptObject;

    private bool opened;

    public bool CanInteract
    {
        get { return !openOnce || !opened; }
    }

    

    Animator anim;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        e_spawn = GetComponent<EnemySpawn>();
        

        col.isTrigger = true;

        if (promptObject != null)
            promptObject.SetActive(false);
    }

    private void Start()
    {
        anim = GetComponent<Animator>();
        anim.SetBool(AnimationStrings.openChest, opened);
    }

    public void SetPromptVisible(bool visible)
    {
        bool canShow = visible && CanInteract;
        if (promptObject != null && promptObject.activeSelf != canShow)
            promptObject.SetActive(canShow);
    }

    public void Interact(PlayerController player)
    {
        if (!CanInteract)
            return;

        opened = true;
        anim.SetBool(AnimationStrings.openChest, opened);
        Sfx.PlayAt(SfxId.WorldChestOpen, transform.position);

        if (promptObject != null)
            promptObject.SetActive(false);

        DropItems();
    }

    public new IInteractable.Type GetType()
    {
        return  IInteractable.Type.Object;
    }

    private void DropItems()
    {
        Vector3 origin = dropPoint != null ? dropPoint.position : transform.position;

        for (int i = 0; i < itemPrefabs.Count; i++)
        {
            Item prefab = itemPrefabs[i].item;
            int cnt = itemPrefabs[i].count;

            for (int j = 0; j < cnt; j++)
            {
                if (prefab == null)
                    continue;

                Vector3 offset = new Vector3(UnityEngine.Random.Range(-spreadX, spreadX), UnityEngine.Random.Range(0f, spreadY), 0f);
                Item droppedItem = Instantiate(prefab, origin, Quaternion.identity);
                droppedItem.itemPrefab = prefab;

                Vector2 desiredPosition = origin + offset;
                Vector2 safePosition = FindSafeDropPosition(droppedItem, desiredPosition, origin);
                droppedItem.transform.position = new Vector3(safePosition.x, safePosition.y, origin.z);
                droppedItem.ConfigureDroppedPhysics(safePosition);

                Rigidbody2D rb = droppedItem.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    float direction = ChooseOpenDirection(safePosition);
                    float minBurst = Mathf.Min(0.5f, burstForce);
                    float horizontalForce = UnityEngine.Random.Range(minBurst, Mathf.Max(minBurst, burstForce));
                    Vector2 force = new Vector2(direction * horizontalForce, Mathf.Max(1f, upwardForce));
                    rb.AddForce(force, ForceMode2D.Impulse);
                }
            }
        }


        if(e_spawn != null)
        {
            e_spawn.StartUp();
        }
    }

    private Vector2 FindSafeDropPosition(Item droppedItem, Vector2 desiredPosition, Vector2 origin)
    {
        if (IsDropPositionFree(droppedItem, desiredPosition)) return desiredPosition;

        const float horizontalStep = 0.4f;
        const float verticalStep = 0.3f;
        for (int row = 1; row <= 8; row++)
        {
            float y = 0.25f + row * verticalStep;
            for (int column = 0; column <= 5; column++)
            {
                if (column == 0)
                {
                    Vector2 centered = origin + new Vector2(0f, y);
                    if (IsDropPositionFree(droppedItem, centered)) return centered;
                    continue;
                }

                Vector2 right = origin + new Vector2(column * horizontalStep, y);
                if (IsDropPositionFree(droppedItem, right)) return right;
                Vector2 left = origin + new Vector2(-column * horizontalStep, y);
                if (IsDropPositionFree(droppedItem, left)) return left;
            }
        }

        // Last resort keeps the item above the chest instead of leaving it embedded in a wall.
        return origin + Vector2.up * 2.75f;
    }

    private static bool IsDropPositionFree(Item droppedItem, Vector2 position)
    {
        droppedItem.transform.position = position;
        Physics2D.SyncTransforms();

        Collider2D ownCollider = droppedItem.GetComponentInChildren<Collider2D>();
        if (ownCollider == null) return true;

        Bounds bounds = ownCollider.bounds;
        Vector2 querySize = new Vector2(
            Mathf.Max(0.12f, bounds.size.x * 0.82f),
            Mathf.Max(0.12f, bounds.size.y * 0.82f));
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(bounds.center, querySize, 0f);
        foreach (Collider2D overlap in overlaps)
        {
            if (overlap == null || overlap == ownCollider || overlap.isTrigger) continue;
            if (overlap.transform.IsChildOf(droppedItem.transform)) continue;
            if (overlap.GetComponentInParent<PlayerController>() != null) continue;
            if (overlap.GetComponentInParent<Item>() != null) continue;
            return false;
        }
        return true;
    }

    private float ChooseOpenDirection(Vector2 origin)
    {
        float leftClearance = GetClearance(origin, Vector2.left);
        float rightClearance = GetClearance(origin, Vector2.right);
        if (Mathf.Abs(leftClearance - rightClearance) > 0.2f)
            return rightClearance > leftClearance ? 1f : -1f;
        return UnityEngine.Random.value < 0.5f ? -1f : 1f;
    }

    private float GetClearance(Vector2 origin, Vector2 direction)
    {
        float maxDistance = Mathf.Max(1.5f, burstForce * 0.65f);
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, maxDistance);
        foreach (RaycastHit2D hit in hits)
        {
            Collider2D hitCollider = hit.collider;
            if (hitCollider == null || hitCollider.isTrigger) continue;
            if (hitCollider.transform.IsChildOf(transform)) continue;
            if (hitCollider.GetComponentInParent<PlayerController>() != null) continue;
            if (hitCollider.GetComponentInParent<Item>() != null) continue;
            return hit.distance;
        }
        return maxDistance;
    }
}
