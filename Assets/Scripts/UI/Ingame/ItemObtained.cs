using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ItemObtained : MonoBehaviour
{
    private static readonly Queue<ItemData> Pending = new Queue<ItemData>();
    public static ItemObtained Instance { get; private set; }
    public static bool IsShowing => Instance != null && Instance.visible;

    private Image icon;
    private TMP_Text itemName;
    private TMP_Text lore;
    private ScrollRect loreScroll;
    private CanvasGroup group;
    private bool visible;
    private float acceptInputAt;
    private float previousTimeScale = 1f;

    public static void Show(Item item)
    {
        Show(ItemData.From(item));
    }

    public static void Show(ItemData item)
    {
        if (item == null) return;
        Pending.Enqueue(item);
        Instance?.DisplayNext();
    }

    public static void ReleaseInstance(ItemObtained candidate)
    {
        if (Instance == candidate) Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        UIRuntime.ConfigureCanvas(gameObject);
        icon = UIRuntime.Find<Image>(transform, "Item_Icon");
        itemName = UIRuntime.FirstText(transform, "Name");
        lore = UIRuntime.FirstText(UIRuntime.Find(transform, "Lore"), "Content");
        ConfigureLoreScroll();
        group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        SetVisible(false);
    }

    private void Start() => DisplayNext();

    private void Update()
    {
        if (!visible || Time.unscaledTime < acceptInputAt) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame) Confirm();
        if (loreScroll != null)
        {
            float delta = 0f;
            Mouse mouse = Mouse.current;
            if (mouse != null) delta += mouse.scroll.ReadValue().y * 0.0015f;
            if (keyboard != null && keyboard.upArrowKey.wasPressedThisFrame) delta += 0.12f;
            if (keyboard != null && keyboard.downArrowKey.wasPressedThisFrame) delta -= 0.12f;
            if (!Mathf.Approximately(delta, 0f))
                loreScroll.verticalNormalizedPosition = Mathf.Clamp01(
                    loreScroll.verticalNormalizedPosition + delta);
        }
    }

    public void DisplayNext()
    {
        if (visible || Pending.Count == 0) return;
        ItemData item = Pending.Dequeue();
        if (icon != null) { icon.sprite = item.icon; icon.enabled = item.icon != null; icon.preserveAspect = true; }
        if (itemName != null) itemName.text = item.itemName;
        if (lore != null) lore.text = item.description;
        UIRuntime.RefreshTextScroll(loreScroll, true);
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        acceptInputAt = Time.unscaledTime + 0.25f;
        SetVisible(true);
        Sfx.Play(SfxId.UiNewItem);
    }

    private void ConfigureLoreScroll()
    {
        Transform loreRoot = UIRuntime.Find(transform, "Lore");
        ScrollRect scroll = loreRoot != null ? loreRoot.GetComponent<ScrollRect>() : null;
        if (scroll == null && loreRoot != null) scroll = loreRoot.GetComponentInChildren<ScrollRect>(true);
        if (scroll == null || scroll.content == null) return;
        loreScroll = scroll;

        UIRuntime.ConfigureTextScroll(scroll, lore, 20f);
    }

    public void Confirm()
    {
        if (!visible) return;
        Sfx.Play(SfxId.UiConfirm);
        SetVisible(false);
        if (Pending.Count > 0) DisplayNext();
        else Time.timeScale = previousTimeScale;
    }

    private void SetVisible(bool value)
    {
        visible = value;
        group.alpha = value ? 1f : 0f;
        group.interactable = value;
        group.blocksRaycasts = value;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (visible) Time.timeScale = previousTimeScale;
    }
}
