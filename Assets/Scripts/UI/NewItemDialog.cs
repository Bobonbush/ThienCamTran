using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Popup "Vật Phẩm Mới" — hiện khi một loại vật phẩm vào túi lần đầu: icon
/// trong khung tròn, tên và hộp cốt truyện trên nền giấy da viền nâu.
/// Tự dựng UI bằng code (kiểu InventoryUI) nên không cần prefab; Inventory
/// gọi sẵn NewItemDialog.Show(item). Nhặt nhiều item mới liên tiếp (mở rương)
/// sẽ xếp hàng hiện lần lượt. Game tạm dừng khi popup mở, nhấn E để đóng.
/// </summary>
public class NewItemDialog : MonoBehaviour
{
    private struct Entry
    {
        public string itemName;
        public string description;
        public Sprite icon;
    }

    private const string FallbackDescription =
        "Đây là cốt truyện của vật phẩm bạn vừa nhặt được, xin chúc mừng.";

    // Bảng màu giấy da / viền nâu (theo mock thiết kế)
    private static readonly Color32 Parchment = new Color32(233, 215, 170, 255);
    private static readonly Color32 ParchmentLight = new Color32(243, 231, 198, 255);
    private static readonly Color32 Brown = new Color32(110, 75, 42, 255);
    private static readonly Color32 TextDark = new Color32(84, 52, 31, 255);

    private static NewItemDialog instance;
    private static readonly Queue<Entry> pending = new Queue<Entry>();

    private static Sprite panelSprite;
    private static Sprite boxSprite;
    private static Sprite ringSprite;
    private static Sprite circleSprite;

    private GameObject overlay;
    private CanvasGroup group;
    private RectTransform panelRect;
    private Image iconImage;
    private TMP_Text nameText;
    private TMP_Text descText;

    private bool visible;
    private float openedAt;
    private float previousTimeScale = 1f;
    private Coroutine animRoutine;

    public static void Show(Item item)
    {
        if (item == null)
            return;

        pending.Enqueue(new Entry
        {
            itemName = item.ItemName,
            description = string.IsNullOrWhiteSpace(item.description) ? FallbackDescription : item.description,
            icon = item.Icon,
        });

        if (instance == null)
        {
            GameObject dialogObject = new GameObject("NewItemDialog");
            instance = dialogObject.AddComponent<NewItemDialog>();
        }

        if (!instance.visible)
            instance.DisplayNext();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildUi();
    }

    private void Update()
    {
        if (!visible)
            return;

        // Chặn chính cú bấm E vừa nhặt item đóng luôn popup cùng frame
        if (Time.unscaledTime < openedAt + 0.35f)
            return;

        bool close = false;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.eKey.wasPressedThisFrame
            || keyboard.spaceKey.wasPressedThisFrame
            || keyboard.enterKey.wasPressedThisFrame))
            close = true;

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            close = true;

        if (!close)
            return;

        if (pending.Count > 0)
        {
            Sfx.Play(SfxId.UiConfirm);
            DisplayNext();
        }
        else
        {
            CloseAll();
        }
    }

    private void DisplayNext()
    {
        Entry entry = pending.Dequeue();

        if (!visible)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        visible = true;
        openedAt = Time.unscaledTime;

        nameText.text = entry.itemName;
        descText.text = entry.description;
        // Giữ khung 110px + preserveAspect: sprite pixel-art nhỏ vẫn phóng to đẹp
        iconImage.sprite = entry.icon;
        iconImage.enabled = entry.icon != null;

        overlay.SetActive(true);
        Sfx.Play(SfxId.UiNewItem);
        StartAnim(AnimateIn());
    }

    private void CloseAll()
    {
        visible = false;
        Time.timeScale = previousTimeScale;
        Sfx.Play(SfxId.UiClose);
        StartAnim(AnimateOut());
    }

    private void StartAnim(IEnumerator routine)
    {
        if (animRoutine != null)
            StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(routine);
    }

    private IEnumerator AnimateIn()
    {
        const float duration = 0.18f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            group.alpha = k;
            panelRect.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, k);
            yield return null;
        }
        group.alpha = 1f;
        panelRect.localScale = Vector3.one;
    }

    private IEnumerator AnimateOut()
    {
        const float duration = 0.14f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            group.alpha = 1f - k;
            panelRect.localScale = Vector3.one * Mathf.Lerp(1f, 0.9f, k);
            yield return null;
        }
        overlay.SetActive(false);
    }

    // ===================== dựng UI =====================

    private void BuildUi()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        BuildSprites();

        overlay = new GameObject("Overlay", typeof(RectTransform), typeof(CanvasGroup));
        overlay.transform.SetParent(transform, false);
        Stretch(overlay.GetComponent<RectTransform>());
        group = overlay.GetComponent<CanvasGroup>();

        // Nền tối phía sau, chặn luôn click xuống game
        Image dim = MakeImage(overlay.transform, "Dim", null, new Color(0f, 0f, 0f, 0.55f));
        Stretch(dim.rectTransform);

        Image panel = MakeImage(overlay.transform, "Panel", panelSprite, Color.white);
        panel.type = Image.Type.Sliced;
        panelRect = panel.rectTransform;
        panelRect.sizeDelta = new Vector2(560f, 680f);

        // Tiêu đề + đường kẻ trang trí hai bên viên kim cương
        MakeText(panelRect, "Title", "Vật Phẩm Mới", 42f, TextDark, FontStyles.Bold,
            new Vector2(0f, 265f), new Vector2(520f, 56f));
        MakeDecor(panelRect, new Vector2(-78f, 212f), new Vector2(110f, 3f), 0f, 1f);
        MakeDecor(panelRect, new Vector2(78f, 212f), new Vector2(110f, 3f), 0f, 1f);
        MakeDecor(panelRect, new Vector2(0f, 212f), new Vector2(12f, 12f), 45f, 1f);

        // Khung tròn giữa: vành nâu + lòng sáng + icon vật phẩm
        Image ring = MakeImage(panelRect, "IconRing", ringSprite, Brown);
        ring.rectTransform.anchoredPosition = new Vector2(0f, 85f);
        ring.rectTransform.sizeDelta = new Vector2(190f, 190f);

        Image iconBg = MakeImage(ring.rectTransform, "IconBg", circleSprite, ParchmentLight);
        iconBg.rectTransform.sizeDelta = new Vector2(168f, 168f);

        iconImage = MakeImage(ring.rectTransform, "Icon", null, Color.white);
        iconImage.rectTransform.sizeDelta = new Vector2(110f, 110f);
        iconImage.preserveAspect = true;

        // Kim cương nhỏ rải quanh cho có không khí "trang kho báu"
        MakeDecor(panelRect, new Vector2(-195f, 205f), new Vector2(10f, 10f), 45f, 0.35f);
        MakeDecor(panelRect, new Vector2(200f, 180f), new Vector2(13f, 13f), 45f, 0.35f);
        MakeDecor(panelRect, new Vector2(-218f, 90f), new Vector2(15f, 15f), 45f, 0.35f);
        MakeDecor(panelRect, new Vector2(222f, 55f), new Vector2(10f, 10f), 45f, 0.35f);
        MakeDecor(panelRect, new Vector2(-188f, -25f), new Vector2(12f, 12f), 45f, 0.35f);
        MakeDecor(panelRect, new Vector2(205f, -5f), new Vector2(14f, 14f), 45f, 0.35f);

        nameText = MakeText(panelRect, "ItemName", "Tên Vật Phẩm", 34f, TextDark, FontStyles.Bold,
            new Vector2(0f, -95f), new Vector2(520f, 46f));

        Image descBox = MakeImage(panelRect, "DescBox", boxSprite, Color.white);
        descBox.type = Image.Type.Sliced;
        descBox.rectTransform.anchoredPosition = new Vector2(0f, -212f);
        descBox.rectTransform.sizeDelta = new Vector2(480f, 175f);

        descText = MakeText(descBox.rectTransform, "DescText", FallbackDescription, 23f, TextDark,
            FontStyles.Normal, Vector2.zero, Vector2.zero);
        Stretch(descText.rectTransform);
        descText.alignment = TextAlignmentOptions.TopLeft;
        descText.margin = new Vector4(24f, 20f, 24f, 20f);

        TMP_Text hint = MakeText(panelRect, "Hint", "Nhấn E để đóng", 18f, TextDark, FontStyles.Italic,
            new Vector2(0f, -313f), new Vector2(400f, 26f));
        hint.color = new Color32(TextDark.r, TextDark.g, TextDark.b, 150);

        overlay.SetActive(false);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Image MakeImage(Transform parent, string name, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    private TMP_Text MakeText(Transform parent, string name, string text, float size, Color color,
        FontStyles style, Vector2 position, Vector2 dimensions)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TMP_Text tmp = textObject.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        RectTransform rect = tmp.rectTransform;
        rect.anchoredPosition = position;
        if (dimensions != Vector2.zero)
            rect.sizeDelta = dimensions;
        return tmp;
    }

    private void MakeDecor(Transform parent, Vector2 position, Vector2 size, float rotation, float alpha)
    {
        Image decor = MakeImage(parent, "Decor", null, new Color32(Brown.r, Brown.g, Brown.b, (byte)(alpha * 255f)));
        decor.raycastTarget = false;
        decor.rectTransform.anchoredPosition = position;
        decor.rectTransform.sizeDelta = size;
        decor.rectTransform.localEulerAngles = new Vector3(0f, 0f, rotation);
    }

    // ===================== sprite procedural =====================

    private static void BuildSprites()
    {
        if (panelSprite != null)
            return;

        panelSprite = RoundedRect(64, 12, 5, Parchment, Brown);
        boxSprite = RoundedRect(64, 10, 4, ParchmentLight, Brown);
        ringSprite = Ring(160, 10);
        circleSprite = Circle(160);
    }

    // Hình chữ nhật bo góc + viền, xuất 9-slice để kéo giãn không méo góc
    private static Sprite RoundedRect(int size, int radius, int border, Color fill, Color line)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x, radius, size - 1 - radius);
                float cy = Mathf.Clamp(y, radius, size - 1 - radius);
                float distance = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));

                Color pixel;
                if (distance > radius)
                    pixel = Color.clear;
                else if (distance > radius - border)
                    pixel = line;
                else
                    pixel = fill;
                tex.SetPixel(x, y, pixel);
            }
        }

        tex.Apply();
        float slice = radius + border + 1;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(slice, slice, slice, slice));
    }

    private static Sprite Ring(int size, int thickness)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float center = (size - 1) * 0.5f;
        float outer = size * 0.5f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                bool onRing = distance <= outer && distance >= outer - thickness;
                tex.SetPixel(x, y, onRing ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite Circle(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float center = (size - 1) * 0.5f;
        float radius = size * 0.5f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                tex.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
