using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public Inventory inventory;
    public GameObject panelRoot;
    public RectTransform contentRoot;

    [Header("Layout")]
    public Vector2 panelSize = new Vector2(360f, 420f);
    public Vector2 slotSize = new Vector2(300f, 34f);

    private bool isOpen;
    private float refreshTimer;

    private void Awake()
    {
        if (inventory == null)
            inventory = GetComponent<Inventory>();

        if (panelRoot == null || contentRoot == null)
            BuildDefaultUI();

        SetOpen(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            Toggle();

        if (!isOpen)
            return;

        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = 0.25f;
            Refresh();
        }
    }

    public void Toggle()
    {
        Sfx.Play(isOpen ? SfxId.UiClose : SfxId.UiOpen);
        SetOpen(!isOpen);
    }

    public void SetOpen(bool open)
    {
        isOpen = open;

        if (panelRoot != null)
            panelRoot.SetActive(isOpen);

        if (isOpen)
            Refresh();
    }

    public void Refresh()
    {
        if (contentRoot == null || inventory == null)
            return;

        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);

        if (inventory.items.Count == 0)
        {
            CreateTextRow("Empty", TextAlignmentOptions.Center);
            return;
        }

        foreach (Inventory.ItemStack stack in inventory.items)
        {
            if (stack == null)
                continue;

            string itemName = string.IsNullOrWhiteSpace(stack.itemName) ? "Item" : stack.itemName;
            CreateTextRow(itemName + " x" + stack.amount, TextAlignmentOptions.Left);
        }
    }

    private void BuildDefaultUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("InventoryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        panelRoot = new GameObject("InventoryPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panelRoot.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.anchoredPosition = new Vector2(-32f, 0f);
        panelRect.sizeDelta = panelSize;

        Image panelImage = panelRoot.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);

        VerticalLayoutGroup panelLayout = panelRoot.GetComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(20, 20, 18, 20);
        panelLayout.spacing = 12f;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;

        TMP_Text title = CreateText(panelRoot.transform, "Inventory", 28f, TextAlignmentOptions.Center);
        LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 40f;

        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contentObject.transform.SetParent(panelRoot.transform, false);
        contentRoot = contentObject.GetComponent<RectTransform>();

        VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 6f;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        LayoutElement contentLayoutElement = contentObject.AddComponent<LayoutElement>();
        contentLayoutElement.flexibleHeight = 1f;
    }

    private void CreateTextRow(string text, TextAlignmentOptions alignment)
    {
        TMP_Text row = CreateText(contentRoot, text, 20f, alignment);
        LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = slotSize.y;
    }

    private TMP_Text CreateText(Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(text, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TMP_Text tmp = textObject.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        return tmp;
    }
}
