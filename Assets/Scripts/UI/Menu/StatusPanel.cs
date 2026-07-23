using System.Collections.Generic;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusPanel : MenuPanel
{
    public override string TabLabel => "Status";
    private const int Columns = 4;
    private readonly List<Transform> slots = new List<Transform>();
    private readonly RectTransform[] equippedHighlighters = new RectTransform[2];
    private readonly Transform[] equipSlots = new Transform[2];
    private List<Inventory.ItemStack> buffs = new List<Inventory.ItemStack>();
    private Inventory inventory;
    private PlayerStats stats;
    private RectTransform selectedHighlighter;
    private RectTransform equipSlotHighlighter;
    private TMP_Text nameText;
    private TMP_Text loreText;
    private TMP_Text hpText;
    private TMP_Text energyText;
    private ScrollRect detailScroll;
    private int selected;
    private int targetEquipSlot;

    private void Awake()
    {
        UIRuntime.ConfigureCanvas(gameObject);
        Transform equipmentPage = transform.Find("EquipmentPage_Panel");
        Transform statusPage = transform.Find("StatusPage_Panel");
        Transform itemsRoot = equipmentPage != null ? equipmentPage.Find("Items") : null;
        Transform content = itemsRoot != null ? itemsRoot.Find("Viewport/Content") : null;

        if (content != null)
        {
            foreach (Transform child in content)
                if (child.name.StartsWith("Item_Slot")) slots.Add(child);
            if (slots.Count > 0)
            {
                Transform template = slots[0];
                while (slots.Count < 48)
                {
                    Transform clone = Instantiate(template, content);
                    clone.name = $"Item_Slot ({slots.Count})";
                    slots.Add(clone);
                }
            }
        }

        selectedHighlighter = itemsRoot != null ? itemsRoot.Find("Select_Highlighter") as RectTransform : null;
        RectTransform equippedTemplate = itemsRoot != null ? itemsRoot.Find("Equipped_Highlighter") as RectTransform : null;
        if (equippedTemplate != null)
        {
            equippedHighlighters[0] = equippedTemplate;
            equippedHighlighters[1] = Instantiate(equippedTemplate, itemsRoot);
            equippedHighlighters[1].name = "Equipped_Highlighter_Slot2";
        }

        Transform characterEquip = statusPage != null ? statusPage.Find("Character_Equip") : null;
        Transform equipRoot = characterEquip != null ? characterEquip.Find("Equip_Slot") : null;
        equipSlots[0] = equipRoot != null ? equipRoot.Find("Slot_1") : null;
        equipSlots[1] = equipRoot != null ? equipRoot.Find("Slot_2") : null;
        equipSlotHighlighter = characterEquip != null ? characterEquip.Find("Select_Highlighter") as RectTransform : null;
        nameText = UIRuntime.FirstText(statusPage, "Name_Equipment");
        loreText = UIRuntime.FirstText(statusPage != null ? statusPage.Find("Equipment_Dialog") : null, "Content");
        Transform detailScrollRoot = statusPage != null ? statusPage.Find("Equipment_Dialog/Scroll View") : null;
        detailScroll = detailScrollRoot != null ? detailScrollRoot.GetComponent<ScrollRect>() : null;
        UIRuntime.ConfigureTextScroll(detailScroll, loreText, 16f);
        hpText = UIRuntime.Find<TMP_Text>(statusPage, "HP");
        energyText = UIRuntime.Find<TMP_Text>(statusPage, "Energy");

        for (int i = 0; i < slots.Count; i++)
        {
            UISlotInput input = slots[i].GetComponent<UISlotInput>() ?? slots[i].gameObject.AddComponent<UISlotInput>();
            input.Bind(i, PointerSelect);
        }
    }

    public override void OnPanelOpened() { BindPlayer(); Refresh(); }

    private void BindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        if (inventory != null) inventory.Changed -= Refresh;
        if (stats != null) stats.Changed -= RefreshStats;
        inventory = player.GetComponent<Inventory>();
        stats = player.GetComponent<PlayerStats>();
        if (inventory != null) inventory.Changed += Refresh;
        if (stats != null) stats.Changed += RefreshStats;
    }

    private void Refresh()
    {
        buffs = inventory != null ? inventory.GetItems(Item.Category.Buff) : new List<Inventory.ItemStack>();
        selected = Mathf.Clamp(selected, 0, Mathf.Max(0, buffs.Count - 1));
        for (int i = 0; i < slots.Count; i++)
        {
            ItemData item = i < buffs.Count ? buffs[i].Data : null;
            UIRuntime.SetSlotIcon(slots[i], item?.icon);
        }

        for (int equipIndex = 0; equipIndex < equipSlots.Length; equipIndex++)
        {
            ItemData equipped = inventory != null && equipIndex < inventory.EquippedBuffs.Count
                ? inventory.EquippedBuffs[equipIndex]
                : null;
            UIRuntime.SetSlotIcon(equipSlots[equipIndex], equipped?.icon, 0.18f);

            int listIndex = equipped == null
                ? -1
                : buffs.FindIndex(stack => stack?.Data != null && stack.Data.id == equipped.id);
            RectTransform marker = equippedHighlighters[equipIndex];
            if (marker != null)
            {
                bool visible = listIndex >= 0 && listIndex < slots.Count;
                marker.gameObject.SetActive(visible);
                if (visible) UIRuntime.FitHighlighterToSlot(marker, slots[listIndex]);
            }
        }

        MoveEquipSlotHighlighter();
        Select(selected);
        RefreshStats();
    }

    private void RefreshStats()
    {
        if (stats == null) return;
        if (hpText != null) hpText.text = $"HP  {stats.Health}/{stats.MaxHealth}";
        if (energyText != null)
            energyText.text = $"Mana  {stats.Mana}/{stats.MaxMana}\nThể lực  {stats.Stamina:0}/{stats.MaxStamina:0}";
    }

    public override void OnNavigate(Vector2 direction)
    {
        if (buffs.Count == 0) return;
        int next = selected;
        if (direction.x != 0) next += direction.x > 0 ? 1 : -1;
        else if (direction.y != 0) next += direction.y > 0 ? -Columns : Columns;
        Select(Mathf.Clamp(next, 0, buffs.Count - 1));
    }

    public override void OnAlternate()
    {
        targetEquipSlot = 1 - targetEquipSlot;
        MoveEquipSlotHighlighter();
    }

    public override void OnSubmit()
    {
        if (inventory != null && stats != null && selected >= 0 && selected < buffs.Count)
            inventory.ToggleBuff(buffs[selected].Data, targetEquipSlot, stats);
    }

    public override void OnScroll(float delta, Vector2 screenPosition)
    {
        UIRuntime.Scroll(detailScroll, delta);
    }

    private void PointerSelect(int index, bool committed)
    {
        if (index < 0) { MoveHighlighter(selected); return; }
        if (index >= buffs.Count) return;
        MoveHighlighter(index);
        if (committed) Select(index);
    }

    private void Select(int index)
    {
        selected = index;
        MoveHighlighter(index);
        ItemData item = index >= 0 && index < buffs.Count ? buffs[index].Data : null;
        if (nameText != null) nameText.text = item != null ? item.itemName : "Trống";
        if (loreText != null) loreText.text = item?.description ?? string.Empty;
        UIRuntime.RefreshTextScroll(detailScroll, true);
    }

    private void MoveHighlighter(int index)
    {
        if (selectedHighlighter == null) return;
        bool valid = index >= 0 && index < buffs.Count && index < slots.Count;
        selectedHighlighter.gameObject.SetActive(valid);
        if (valid) UIRuntime.FitHighlighterToSlot(selectedHighlighter, slots[index]);
    }

    private void MoveEquipSlotHighlighter()
    {
        if (equipSlotHighlighter == null || equipSlots[targetEquipSlot] == null) return;
        equipSlotHighlighter.gameObject.SetActive(true);
        UIRuntime.FitHighlighterToSlot(equipSlotHighlighter, equipSlots[targetEquipSlot]);
    }

    private void OnDestroy()
    {
        if (inventory != null) inventory.Changed -= Refresh;
        if (stats != null) stats.Changed -= RefreshStats;
    }
}
