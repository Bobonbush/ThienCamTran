using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UIRuntime
{
    public static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        foreach (Transform child in root)
        {
            if (child.name == name) return child;
            Transform found = Find(child, name);
            if (found != null) return found;
        }
        return null;
    }

    public static T Find<T>(Transform root, string name) where T : Component
    {
        Transform found = Find(root, name);
        return found != null ? found.GetComponent<T>() : null;
    }

    public static void ConfigureCanvas(GameObject root)
    {
        CanvasScaler scaler = root.GetComponentInChildren<CanvasScaler>(true);
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            // Text inside a ScrollRect must keep its authored point size so its preferred
            // height can exceed the viewport. Auto-sizing it here would shrink the lore
            // until it fits and leave the ScrollRect with nothing to scroll.
            if (FindParentScrollRect(text.transform) != null) continue;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(8f, text.fontSize * 0.55f);
            text.fontSizeMax = Mathf.Max(text.fontSize, text.fontSizeMax);
        }
    }

    public static void ConfigureTextScroll(ScrollRect scroll, TMP_Text text, float fontSize)
    {
        if (scroll == null || scroll.content == null || text == null) return;

        scroll.enabled = true;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.scrollSensitivity = 32f;

        text.enableAutoSizing = false;
        text.fontSize = fontSize;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = true;

        RectTransform content = scroll.content;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(content.pivot.x, 1f);

        ContentSizeFitter contentFitter = content.GetComponent<ContentSizeFitter>();
        if (contentFitter == null)
            contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (text.rectTransform != content)
        {
            ContentSizeFitter textFitter = text.GetComponent<ContentSizeFitter>();
            if (textFitter == null)
                textFitter = text.gameObject.AddComponent<ContentSizeFitter>();
            textFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        if (scroll.viewport != null)
        {
            Image viewportImage = scroll.viewport.GetComponent<Image>();
            if (viewportImage != null) viewportImage.raycastTarget = true;
        }

        RefreshTextScroll(scroll, true);
    }

    public static void RefreshTextScroll(ScrollRect scroll, bool resetToTop = false)
    {
        if (scroll == null || scroll.content == null) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        Canvas.ForceUpdateCanvases();
        if (resetToTop) scroll.verticalNormalizedPosition = 1f;
    }

    public static void Scroll(ScrollRect scroll, float wheelDelta)
    {
        if (scroll == null || !scroll.vertical || Mathf.Approximately(wheelDelta, 0f)) return;
        float direction = Mathf.Sign(wheelDelta);
        scroll.verticalNormalizedPosition = Mathf.Clamp01(
            scroll.verticalNormalizedPosition + direction * 0.09f);
    }

    public static bool ContainsScreenPoint(ScrollRect scroll, Vector2 screenPoint)
    {
        RectTransform area = scroll != null ? scroll.viewport : null;
        return area != null && RectTransformUtility.RectangleContainsScreenPoint(area, screenPoint);
    }

    public static void ConfigureSingleLineListText(TMP_Text text, float maxSize = 18f)
    {
        if (text == null) return;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = maxSize;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.maxVisibleLines = 1;
    }

    private static ScrollRect FindParentScrollRect(Transform child)
    {
        for (Transform current = child; current != null; current = current.parent)
        {
            ScrollRect scroll = current.GetComponent<ScrollRect>();
            if (scroll != null) return scroll;
        }
        return null;
    }

    public static void SetIcon(Transform slot, Sprite sprite)
    {
        if (slot == null) return;
        Image[] images = slot.GetComponentsInChildren<Image>(true);
        Image target = Array.Find(images, image => image.transform != slot &&
            (image.name.IndexOf("Icon", StringComparison.OrdinalIgnoreCase) >= 0 || !image.raycastTarget));
        if (target == null && images.Length > 1) target = images[images.Length - 1];
        if (target == null) target = slot.GetComponent<Image>();
        if (target == null) return;
        target.sprite = sprite;
        target.enabled = sprite != null;
        target.preserveAspect = true;
    }

    public static Image SetSlotIcon(Transform slot, Sprite sprite, float padding = 0.12f)
    {
        if (slot == null) return null;
        Transform existing = slot.Find("Runtime_Icon");
        Image icon = existing != null ? existing.GetComponent<Image>() : null;
        if (icon == null)
        {
            GameObject iconObject = new GameObject("Runtime_Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(slot, false);
            RectTransform rect = (RectTransform)iconObject.transform;
            rect.anchorMin = new Vector2(padding, padding);
            rect.anchorMax = new Vector2(1f - padding, 1f - padding);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            icon = iconObject.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
        }
        icon.sprite = sprite;
        icon.enabled = sprite != null;
        icon.transform.SetAsFirstSibling();
        return icon;
    }

    public static void FitHighlighterToSlot(RectTransform highlighter, Transform slot)
    {
        if (highlighter == null || slot == null) return;
        highlighter.SetParent(slot, false);
        highlighter.anchorMin = Vector2.zero;
        highlighter.anchorMax = Vector2.one;
        highlighter.offsetMin = Vector2.zero;
        highlighter.offsetMax = Vector2.zero;
        highlighter.localScale = Vector3.one;
        highlighter.SetAsLastSibling();
        foreach (Image image in highlighter.GetComponentsInChildren<Image>(true))
            image.raycastTarget = false;
    }

    public static TMP_Text FirstText(Transform root, params string[] preferredNames)
    {
        foreach (string preferred in preferredNames)
        {
            TMP_Text found = Find<TMP_Text>(root, preferred);
            if (found != null) return found;
        }
        return root != null ? root.GetComponentInChildren<TMP_Text>(true) : null;
    }
}

public class UISlotInput : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Action<int, bool> select;
    private int index;

    public void Bind(int slotIndex, Action<int, bool> callback)
    {
        index = slotIndex;
        select = callback;
        Image image = GetComponent<Image>();
        if (image != null) image.raycastTarget = true;
    }

    public void OnPointerEnter(PointerEventData eventData) => select?.Invoke(index, false);
    public void OnPointerExit(PointerEventData eventData) => select?.Invoke(-1, false);
    public void OnPointerClick(PointerEventData eventData) => select?.Invoke(index, true);
}
