using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class InventoryDatabase : MonoBehaviour
{
    public static InventoryDatabase Instance { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]
    private List<Item> allItems;

    public Item GetItemByName(string itemName)
    {
        return allItems.Find(item => item != null && item.itemName == itemName);
    }

    public ItemData GetItemDataByName(string itemName)
    {
        Item item = GetItemByName(itemName);
        Item source = item.InventoryPrefab;
        ItemData collected = ItemData.From(source != null ? source : item);

        return collected;
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this.gameObject);
    }


}
