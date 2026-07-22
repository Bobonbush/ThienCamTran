using System.Collections.Generic;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanel : MenuPanel
{
    public override string TabLabel => "Inventory";
    private const int Columns = 4;
    private readonly List<Transform> slots = new List<Transform>();
    private readonly List<TMP_Text> amountTexts = new List<TMP_Text>();
    private List<Inventory.ItemStack> foods = new List<Inventory.ItemStack>();
    private Inventory inventory;
    private PlayerStats stats;
    private RectTransform highlighter;
    private Transform detailIconSlot;
    private TMP_Text detailName;
    private TMP_Text detailStats;
    private TMP_Text detailLore;
    private ScrollRect itemScroll;
    private ScrollRect detailScroll;
    private int selected;

    private void Awake()
    {
        UIRuntime.ConfigureCanvas(gameObject);
        Transform listPage = transform.Find("InventoryPage_Panel");
        Transform detailPage = transform.Find("ItemPage_Panel");
        Transform itemsRoot = listPage != null ? listPage.Find("Items") : null;
        Transform content = itemsRoot != null ? itemsRoot.Find("Viewport/Content") : null;
        itemScroll = itemsRoot != null ? itemsRoot.GetComponent<ScrollRect>() : null;

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

        highlighter = itemsRoot != null ? itemsRoot.Find("Select_Highlighter") as RectTransform : null;
        Transform itemInfo = detailPage != null ? detailPage.Find("Item_Info") : null;
        detailIconSlot = itemInfo != null ? itemInfo.Find("Icon_Slot") : null;
        detailName = UIRuntime.FirstText(itemInfo, "Name");
        detailStats = UIRuntime.FirstText(itemInfo, "Attribute_Title");
        detailLore = UIRuntime.FirstText(itemInfo != null ? itemInfo.Find("Item_Dialog") : null, "Content");
        Transform detailScrollRoot = itemInfo != null ? itemInfo.Find("Item_Dialog/Scroll View") : null;
        detailScroll = detailScrollRoot != null ? detailScrollRoot.GetComponent<ScrollRect>() : null;
        UIRuntime.ConfigureTextScroll(detailScroll, detailLore, 16f);
        UIRuntime.ConfigureSingleLineListText(detailStats, 18f);
        if (itemScroll != null) itemScroll.scrollSensitivity = 32f;

        TMP_FontAsset font = GetComponentInChildren<TMP_Text>(true)?.font;
        for (int i = 0; i < slots.Count; i++)
        {
            amountTexts.Add(CreateAmountText(slots[i], font));
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
        inventory = player.GetComponent<Inventory>();
        stats = player.GetComponent<PlayerStats>();
        if (inventory != null) inventory.Changed += Refresh;
    }

    private void Refresh()
    {
        foods = inventory != null ? inventory.GetItems(Item.Category.Food) : new List<Inventory.ItemStack>();
        selected = Mathf.Clamp(selected, 0, Mathf.Max(0, foods.Count - 1));
        for (int i = 0; i < slots.Count; i++)
        {
            bool occupied = i < foods.Count && foods[i].Data != null;
            UIRuntime.SetSlotIcon(slots[i], occupied ? foods[i].Data.icon : null);
            amountTexts[i].text = occupied ? $"x{foods[i].amount}" : string.Empty;
            amountTexts[i].gameObject.SetActive(occupied);
        }
        ShowDetail(selected);
        MoveHighlighter(selected);
    }

    public override void OnNavigate(Vector2 direction)
    {
        if (foods.Count == 0) return;
        int next = selected;
        if (direction.x != 0) next += direction.x > 0 ? 1 : -1;
        else if (direction.y != 0) next += direction.y > 0 ? -Columns : Columns;
        Select(Mathf.Clamp(next, 0, foods.Count - 1));
    }

    public override void OnSubmit()
    {
        if (inventory != null && stats != null && selected >= 0 && selected < foods.Count)
            inventory.Consume(foods[selected].Data, stats);
    }

    public override void OnScroll(float delta, Vector2 screenPosition)
    {
        ScrollRect target = UIRuntime.ContainsScreenPoint(detailScroll, screenPosition)
            ? detailScroll
            : itemScroll;
        UIRuntime.Scroll(target, delta);
    }

    private void PointerSelect(int index, bool committed)
    {
        if (index < 0) { MoveHighlighter(selected); return; }
        if (index >= foods.Count) return;
        MoveHighlighter(index);
        if (committed) Select(index);
    }

    private void Select(int index)
    {
        selected = index;
        MoveHighlighter(index);
        ShowDetail(index);
        EnsureVisible(index);
    }

    private void MoveHighlighter(int index)
    {
        if (highlighter == null) return;
        bool valid = index >= 0 && index < foods.Count && index < slots.Count;
        highlighter.gameObject.SetActive(valid);
        if (valid) UIRuntime.FitHighlighterToSlot(highlighter, slots[index]);
    }

    private void ShowDetail(int index)
    {
        ItemData item = index >= 0 && index < foods.Count ? foods[index].Data : null;
        UIRuntime.SetSlotIcon(detailIconSlot, item?.icon, 0.05f);
        if (detailName != null) detailName.text = item != null ? item.itemName : "Trống";
        if (detailStats != null)
            detailStats.text = item != null
                ? $"HP +{item.healthRestore}  Mana +{item.manaRestore}  Thể lực +{item.staminaRestore:0}"
                : string.Empty;
        if (detailLore != null) detailLore.text = item?.description ?? string.Empty;
        UIRuntime.RefreshTextScroll(detailScroll, true);
    }

    private static TMP_Text CreateAmountText(Transform slot, TMP_FontAsset font)
    {
        Transform existing = slot.Find("Runtime_Amount");
        if (existing != null) return existing.GetComponent<TMP_Text>();
        GameObject textObject = new GameObject("Runtime_Amount", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(slot, false);
        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(2f, 1f);
        rect.offsetMax = new Vector2(-3f, -2f);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.font = font;
        text.fontSize = 16f;
        text.alignment = TextAlignmentOptions.BottomRight;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private void EnsureVisible(int index)
    {
        if (itemScroll == null || itemScroll.viewport == null || index < 0 || index >= slots.Count) return;
        Canvas.ForceUpdateCanvases();
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(itemScroll.viewport, slots[index]);
        Rect view = itemScroll.viewport.rect;
        Vector2 position = itemScroll.content.anchoredPosition;
        if (bounds.max.y > view.yMax) position.y -= bounds.max.y - view.yMax;
        else if (bounds.min.y < view.yMin) position.y += view.yMin - bounds.min.y;
        itemScroll.content.anchoredPosition = position;
    }

    private void OnDestroy() { if (inventory != null) inventory.Changed -= Refresh; }
}
