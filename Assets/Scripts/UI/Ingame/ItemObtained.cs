using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
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
    bool IsAnyGamepadButtonPressed()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null) return false;

        // Loop through all controls on the gamepad (buttons, triggers, bumpers, stick presses)
        foreach (var control in gamepad.allControls)
        {
            if (control is ButtonControl button && button.wasPressedThisFrame)
            {
                return true;
            }
        }
        return false;
    }

    private void Start() => DisplayNext();
    private bool JustStarted = false;

    private void Update()
    {
        if (!visible || Time.unscaledTime < acceptInputAt) return;
        Keyboard keyboard = Keyboard.current;

        bool receiveValidInput = false;
        if (loreScroll != null)
        {
            float delta = 0f;
            Mouse mouse = Mouse.current;
            float scroll = mouse.scroll.ReadValue().y;

            if (!Mathf.Approximately(scroll, 0f))
            {
                delta += scroll * 0.0015f;
                receiveValidInput = true;
            }
            if (InputManager.Instance.UpPress && !receiveValidInput)
            {
                receiveValidInput = true;
                delta += 0.12f;
            }
            if (InputManager.Instance.DownPress && !receiveValidInput)
            {
                receiveValidInput = true;
                delta -= 0.12f;
            }


            if (!Mathf.Approximately(delta, 0f))
                loreScroll.verticalNormalizedPosition = Mathf.Clamp01(
                    loreScroll.verticalNormalizedPosition + delta);
        }

        if (keyboard != null && Keyboard.current.anyKey.wasPressedThisFrame && JustStarted == false && !receiveValidInput)
        {
            receiveValidInput = true;
            Confirm();
        }
        if (Gamepad.current != null && IsAnyGamepadButtonPressed() && JustStarted == false && !receiveValidInput)
        {
            receiveValidInput = true;
            Confirm();
        }
        JustStarted = false;
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
        JustStarted = true;

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
