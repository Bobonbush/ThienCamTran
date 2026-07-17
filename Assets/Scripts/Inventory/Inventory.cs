using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Serializable]
    public class ItemStack
    {
        public Item itemPrefab;
        public string itemName;
        public int amount;
    }

    public int maxSlots = 24;
    public List<ItemStack> items = new List<ItemStack>();

    public bool AddItem(Item item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        Item prefab = item.InventoryPrefab;
        foreach (ItemStack stack in items)
        {
            // Stack khôi phục từ save chỉ có tên (prefab null) — gộp theo tên
            // và gắn lại prefab ở lần nhặt đầu tiên sau khi load
            bool samePrefab = stack.itemPrefab == prefab;
            bool sameRestoredName = stack.itemPrefab == null && stack.itemName == item.ItemName;
            if (samePrefab || sameRestoredName)
            {
                stack.amount += amount;
                if (stack.itemPrefab == null)
                    stack.itemPrefab = prefab;
                return true;
            }
        }

        if (items.Count >= maxSlots)
        {
            Debug.Log("Inventory full");
            return false;
        }

        items.Add(new ItemStack
        {
            itemPrefab = prefab,
            itemName = item.ItemName,
            amount = amount
        });

        // Lần đầu loại vật phẩm này vào túi -> popup giới thiệu
        NewItemDialog.Show(item);

        return true;
    }
}
