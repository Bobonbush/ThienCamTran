using System.Collections.Generic;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DatabasePanel : MenuPanel
{
    private struct RectLayout
    {
        public Vector2 anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta;
        public Vector3 scale;
        public static RectLayout Capture(RectTransform rect) => new RectLayout
        {
            anchorMin = rect.anchorMin, anchorMax = rect.anchorMax, pivot = rect.pivot,
            anchoredPosition = rect.anchoredPosition, sizeDelta = rect.sizeDelta, scale = rect.localScale
        };
        public void Apply(RectTransform rect)
        {
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition; rect.sizeDelta = sizeDelta; rect.localScale = scale;
        }
    }

    public override string TabLabel => "Database";
    private readonly List<Transform> rows = new List<Transform>();
    private List<Inventory.ItemStack> loreItems = new List<Inventory.ItemStack>();
    private Inventory inventory;
    private RectTransform boxHighlighter;
    private RectTransform arrowHighlighter;
    private RectLayout boxLayout;
    private RectLayout arrowLayout;
    private Transform detailIconSlot;
    private TMP_Text detailName;
    private TMP_Text detailLore;
    private ScrollRect worldScroll;
    private ScrollRect detailScroll;
    private int selected;

    private void Awake()
    {
        UIRuntime.ConfigureCanvas(gameObject);
        Transform world = transform.Find("WorldPage_Panel");
        Transform detail = transform.Find("DetailPage_Panel");
        Transform content = world != null ? world.Find("Content_Panel/Scroll View/Viewport/Content") : null;
        Transform worldScrollRoot = world != null ? world.Find("Content_Panel/Scroll View") : null;
        worldScroll = worldScrollRoot != null ? worldScrollRoot.GetComponent<ScrollRect>() : null;

        if (content != null)
        {
            foreach (Transform child in content)
                if (child.GetComponentInChildren<TMP_Text>(true) != null) rows.Add(child);

            if (rows.Count > 0)
            {
                boxHighlighter = rows[0].Find("Highlighter_Box") as RectTransform;
                arrowHighlighter = rows[0].Find("Highlighter_Arrow") as RectTransform;
                if (boxHighlighter != null) boxLayout = RectLayout.Capture(boxHighlighter);
                if (arrowHighlighter != null) arrowLayout = RectLayout.Capture(arrowHighlighter);

                Transform template = rows[0];
                while (rows.Count < 48)
                {
                    Transform clone = Instantiate(template, content);
                    clone.name = $"Lore_Row_{rows.Count}";
                    RectTransform cloneBox = clone.Find("Highlighter_Box") as RectTransform;
                    RectTransform cloneArrow = clone.Find("Highlighter_Arrow") as RectTransform;
                    if (cloneBox != null) Destroy(cloneBox.gameObject);
                    if (cloneArrow != null) Destroy(cloneArrow.gameObject);
                    rows.Add(clone);
                }
            }
        }

        Transform detailContent = detail != null ? detail.Find("Detail_Content") : null;
        detailIconSlot = detailContent != null ? detailContent.Find("Icon_Slot") : null;
        detailName = UIRuntime.FirstText(detail, "Content_Title");
        detailLore = UIRuntime.FirstText(detailContent != null ? detailContent.Find("LorePanel") : null, "Content");
        Transform detailScrollRoot = detailContent != null ? detailContent.Find("LorePanel/Scroll View") : null;
        detailScroll = detailScrollRoot != null ? detailScrollRoot.GetComponent<ScrollRect>() : null;
        UIRuntime.ConfigureTextScroll(detailScroll, detailLore, 16f);
        if (worldScroll != null) worldScroll.scrollSensitivity = 32f;
        DisableHighlighterRaycasts(boxHighlighter);
        DisableHighlighterRaycasts(arrowHighlighter);

        for (int i = 0; i < rows.Count; i++)
        {
            UIRuntime.ConfigureSingleLineListText(rows[i].GetComponentInChildren<TMP_Text>(true), 17f);
            UISlotInput input = rows[i].GetComponent<UISlotInput>() ?? rows[i].gameObject.AddComponent<UISlotInput>();
            input.Bind(i, PointerSelect);
        }
    }

    public override void OnPanelOpened()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (inventory != null) inventory.Changed -= Refresh;
        inventory = player != null ? player.GetComponent<Inventory>() : null;
        if (inventory != null) inventory.Changed += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        loreItems = inventory != null ? inventory.GetItems(Item.Category.Lore) : new List<Inventory.ItemStack>();
        selected = Mathf.Clamp(selected, 0, Mathf.Max(0, loreItems.Count - 1));
        for (int i = 0; i < rows.Count; i++)
        {
            bool occupied = i < loreItems.Count && loreItems[i].Data != null;
            rows[i].gameObject.SetActive(occupied);
            TMP_Text text = rows[i].GetComponentInChildren<TMP_Text>(true);
            if (text != null) text.text = occupied ? loreItems[i].Data.itemName : string.Empty;
        }
        Select(selected);
    }

    public override void OnNavigate(Vector2 direction)
    {
        if (loreItems.Count == 0 || direction.y == 0) return;
        Select(Mathf.Clamp(selected + (direction.y > 0 ? -1 : 1), 0, loreItems.Count - 1));
    }

    public override void OnScroll(float delta, Vector2 screenPosition)
    {
        ScrollRect target = UIRuntime.ContainsScreenPoint(detailScroll, screenPosition)
            ? detailScroll
            : worldScroll;
        UIRuntime.Scroll(target, delta);
    }

    private void PointerSelect(int index, bool committed)
    {
        if (index < 0) { MoveHighlighters(selected); return; }
        if (index >= loreItems.Count) return;
        MoveHighlighters(index);
        if (committed) Select(index);
    }

    private void Select(int index)
    {
        selected = index;
        MoveHighlighters(index);
        ItemData item = index >= 0 && index < loreItems.Count ? loreItems[index].Data : null;
        UIRuntime.SetSlotIcon(detailIconSlot, item?.icon, 0.06f);
        if (detailName != null) detailName.text = item != null ? item.itemName : "Chưa có cốt truyện";
        if (detailLore != null) detailLore.text = item?.description ?? string.Empty;
        UIRuntime.RefreshTextScroll(detailScroll, true);
    }

    private void MoveHighlighters(int index)
    {
        bool valid = index >= 0 && index < loreItems.Count && index < rows.Count;
        Move(boxHighlighter, boxLayout, index, valid);
        Move(arrowHighlighter, arrowLayout, index, valid);
    }

    private void Move(RectTransform highlighter, RectLayout layout, int index, bool valid)
    {
        if (highlighter == null) return;
        highlighter.gameObject.SetActive(valid);
        if (!valid) return;
        highlighter.SetParent(rows[index], false);
        layout.Apply(highlighter);
        highlighter.SetAsLastSibling();
    }

    private static void DisableHighlighterRaycasts(RectTransform highlighter)
    {
        if (highlighter == null) return;
        foreach (UnityEngine.UI.Graphic graphic in highlighter.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            graphic.raycastTarget = false;
    }

    private void OnDestroy() { if (inventory != null) inventory.Changed -= Refresh; }
}
