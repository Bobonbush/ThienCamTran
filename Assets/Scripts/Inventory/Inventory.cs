using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Serializable]
    public class ItemStack
    {
        // Retained for compatibility with existing saves/Inspector data. Runtime UI uses the
        // snapshot because a scene Item component becomes null after its pickup object is destroyed.
        public Item itemPrefab;
        public string itemName;
        public int amount;
        [SerializeField] private ItemData data;

        public bool isEquipped = false;

        public ItemData Data
        {
            get
            {
                if (data == null && itemPrefab != null)
                    data = ItemData.From(itemPrefab);
                return data;
            }
            set => data = value;
        }
    }

    public event Action Changed;

    public int maxSlots = 48;
    public List<ItemStack> items = new List<ItemStack>();
    [SerializeField] private ItemData[] equippedBuffs = new ItemData[2];
    [SerializeField] private int score;

    public IReadOnlyList<ItemData> EquippedBuffs
    {
        get { 
            EnsureEquipmentSlots(); 
            return equippedBuffs; 
        }
    }
    public int Score => score;


    public bool AddItem(Item item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        Item source = item.InventoryPrefab;
        ItemData collected = ItemData.From(source != null ? source : item);
        if (collected == null) return false;

        if (collected.category == Item.Category.Currency)
        {
            score += amount;
            Changed?.Invoke();
            return true;
        }

        foreach (ItemStack stack in items)
        {
            if (stack?.Data == null || stack.Data.id != collected.id) continue;
            stack.amount += amount;
            Changed?.Invoke();
            return true;
        }

        if (items.Count >= maxSlots)
        {
            Debug.Log("Inventory full");
            return false;
        }

        items.Add(new ItemStack
        {
            itemPrefab = source,
            itemName = collected.itemName,
            amount = amount,
            Data = collected
        });
        Changed?.Invoke();
        ItemObtained.Show(collected);
        return true;
    }

    public List<ItemStack> GetItems(Item.Category category)
    {
        return items.FindAll(stack => stack?.Data != null && stack.Data.category == category);
    }

    public bool Consume(ItemData item, PlayerStats stats)
    {
        if (item == null || stats == null || item.category != Item.Category.Food) return false;
        ItemStack stack = items.Find(entry => entry?.Data != null && entry.Data.id == item.id);
        if (stack == null || stack.amount <= 0 || !stats.ConsumeFood(item)) return false;
        stack.amount--;
        if (stack.amount <= 0) items.Remove(stack);
        Changed?.Invoke();
        return true;
    }

    public bool ToggleBuff(ItemData item, int slot, PlayerStats stats)
    {
        if (item == null || stats == null || item.category != Item.Category.Buff) return false;
        EnsureEquipmentSlots();
        slot = Mathf.Clamp(slot, 0, equippedBuffs.Length - 1);

        ItemStack targetStack = items.Find(entry => entry?.Data != null && entry.Data.id == item.id);
        if (targetStack == null) return false;

        int equippedIndex = Array.FindIndex(equippedBuffs, equipped => equipped != null && equipped.id == item.id);
        
        if (equippedIndex >= 0)
        {
            equippedBuffs[equippedIndex] = null;
            targetStack.isEquipped = false;

        }
        else
        {
            ItemData existingInSlot = equippedBuffs[slot];
            if (existingInSlot != null)
            {
                ItemStack existingStack = items.Find(entry => entry?.Data != null && entry.Data.id == existingInSlot.id);
                if (existingStack != null)
                {
                    existingStack.isEquipped = false;
                }
            }

            // Assign to slot and mark stack as equipped
            equippedBuffs[slot] = item;
            targetStack.isEquipped = true;
        }
        stats.ApplyEquipment(equippedBuffs);
        Changed?.Invoke();
        return true;
    }

    public bool IsEquipped(ItemData item)
    {
        EnsureEquipmentSlots();
        return item != null && Array.Exists(equippedBuffs,
            equipped => equipped != null && equipped.id == item.id);
    }

    private void EnsureEquipmentSlots()
    {
        if (equippedBuffs != null && equippedBuffs.Length == 2) return;
        ItemData[] restored = new ItemData[2];
        if (equippedBuffs != null)
            Array.Copy(equippedBuffs, restored, Mathf.Min(equippedBuffs.Length, restored.Length));
        equippedBuffs = restored;
    }

    public void RestoreEquippedBuffs()
    {
        EnsureEquipmentSlots();

        // Clear array first
        Array.Clear(equippedBuffs, 0, equippedBuffs.Length);

        int slotIndex = 0;
        foreach (ItemStack stack in items)
        {
            if (stack.isEquipped && stack.Data != null && stack.Data.category == Item.Category.Buff)
            {
                if (slotIndex < equippedBuffs.Length)
                {
                    equippedBuffs[slotIndex] = stack.Data;
                    slotIndex++;

                }
                else
                {
                    stack.isEquipped = false;
                }
            }
        }

        gameObject.GetComponent<PlayerStats>().ApplyEquipment(equippedBuffs);
        Changed?.Invoke();
    }
}
