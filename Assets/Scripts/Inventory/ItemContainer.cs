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
                Item droppedItem = Instantiate(prefab, origin + offset, Quaternion.identity);
                droppedItem.itemPrefab = prefab;

                Rigidbody2D rb = droppedItem.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    float direction = UnityEngine.Random.value < 0.5f ? -1f : 1f;
                    Vector2 force = new Vector2(direction * UnityEngine.Random.Range(0.5f, burstForce), upwardForce);
                    rb.AddForce(force, ForceMode2D.Impulse);
                }
            }
        }
    }
}
