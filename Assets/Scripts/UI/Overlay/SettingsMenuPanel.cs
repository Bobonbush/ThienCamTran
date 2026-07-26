using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Setting tab of the in-game book overlay. Instantiates the same authored "Option_Panel "
    /// prefab the main menu uses and drives it with the same <see cref="MainMenuOptionsPanel"/>
    /// binder, so the two screens cannot drift apart. Settings themselves live in
    /// <see cref="SettingsService"/> (PlayerPrefs, shared by every screen), so nothing needs syncing.
    ///
    /// Also owns the "exit to main menu" flow, which only makes sense in-game and therefore stays
    /// out of the shared binder.
    /// </summary>
    public sealed class SettingsMenuPanel : MenuPanel, IOptionsHost
    {
        [Header("Panel content")]
        [Tooltip("Assets/Prefabs/Option_Panel .prefab — the same settings art the main menu shows.")]
        [SerializeField] private GameObject optionPanelPrefab;

        [Header("UI artwork")]
        [SerializeField] private Sprite selectedButtonSprite;
        [SerializeField] private Sprite videoLabelSprite;
        [SerializeField] private Sprite audioLabelSprite;
        [SerializeField] private Sprite controlLabelSprite;

        [Header("Backend services")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private InputActionAsset inputActions;

        [Header("Exit")]
        [SerializeField] private string exitButtonLabel = "THOÁT VỀ MENU";
        [SerializeField, TextArea]
        private string exitConfirmMessage = "Thoát về menu chính?\nTiến trình từ lần cầu nguyện cuối sẽ mất.";
        [SerializeField] private string exitConfirmYes = "CÓ";
        [SerializeField] private string exitConfirmNo = "KHÔNG";

        private MainMenuOptionsPanel options;
        private GameObject optionPanelInstance;
        private GameObject exitConfirm;

        public override string TabLabel => "Setting";

        public AudioMixer AudioMixer => audioMixer;
        public InputActionAsset InputActions => inputActions;
        public Sprite VideoLabelSprite => videoLabelSprite;
        public Sprite AudioLabelSprite => audioLabelSprite;
        public Sprite ControlLabelSprite => controlLabelSprite;
        public Sprite SelectedButtonSprite => selectedButtonSprite;

        public override void OnPanelOpened()
        {
            EnsureBuilt();
            HideExitConfirm();
            options?.Open();
        }

        public override void OnPanelClosed()
        {
            HideExitConfirm();
            // Same contract as the main menu's Back: unsaved rebinds and brightness revert.
            options?.CancelUncommitted();
        }

        /// <summary>
        /// Built on first open rather than in Awake: ConfigureControls clones a row per binding,
        /// which is wasted work in every scene the player never opens this tab.
        /// </summary>
        private void EnsureBuilt()
        {
            if (options != null) return;

            if (optionPanelPrefab == null)
            {
                Debug.LogError(
                    "SettingsMenuPanel: optionPanelPrefab is not assigned. " +
                    "Run Tools/Game UI/Configure Menu Overlay.", this);
                return;
            }

            optionPanelInstance = Instantiate(optionPanelPrefab, transform);
            optionPanelInstance.name = "Option_Panel";
            StretchToParent(optionPanelInstance.transform as RectTransform);
            optionPanelInstance.SetActive(true);

            options = optionPanelInstance.GetComponent<MainMenuOptionsPanel>();
            if (options == null) options = optionPanelInstance.AddComponent<MainMenuOptionsPanel>();
            options.Initialize(this);

            // After Initialize, so its Find calls cannot pick up the clones below.
            BuildExitUi();
        }

        /// <summary>
        /// Clones the authored Save_Button / Save_Dialog instead of adding new art, the same way
        /// MainMenuOptionsPanel.BuildGamepadCell clones the authored key cell.
        /// </summary>
        private void BuildExitUi()
        {
            RectTransform saveButton = UIRuntime.Find<RectTransform>(optionPanelInstance.transform, "Save_Button");
            RectTransform resetButton = UIRuntime.Find<RectTransform>(optionPanelInstance.transform, "Reset_Button");
            if (saveButton == null) return;

            RectTransform exitButton = Instantiate(saveButton.gameObject, saveButton.parent).transform as RectTransform;
            exitButton.name = "Exit_Button";

            // The authored footer only has room for two buttons, so re-space the row into thirds.
            // Only this in-game copy of the prefab is touched; the main menu keeps its own layout.
            const float bottom = 0.0746f;
            const float top = 0.1689f;
            if (resetButton != null) SetAnchors(resetButton, 0.055f, bottom, 0.355f, top);
            SetAnchors(saveButton, 0.365f, bottom, 0.665f, top);
            SetAnchors(exitButton, 0.675f, bottom, 0.975f, top);

            SetLabel(exitButton, exitButtonLabel);
            // EnsureButton clears the SaveCurrent listener inherited from the cloned original.
            UIRuntime.EnsureButton(exitButton, ShowExitConfirm, selectedButtonSprite, true);

            BuildExitConfirm(saveButton);
        }

        /// <summary>
        /// Builds the confirm as a full-page overlay on this panel rather than inside the authored
        /// Save_Dialog toast, which is a fixed 343x90 strip pinned near the bottom — far too small
        /// for the message, and it would sit under the settings content.
        /// </summary>
        private void BuildExitConfirm(RectTransform buttonTemplate)
        {
            RectTransform dialogTemplate =
                UIRuntime.Find<RectTransform>(optionPanelInstance.transform, "Save_Dialog");
            if (dialogTemplate == null) return;

            // Backdrop also swallows clicks meant for the settings behind it.
            GameObject backdrop = new GameObject("Exit_Confirm", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(transform, false);
            RectTransform backdropRect = (RectTransform)backdrop.transform;
            StretchToParent(backdropRect);
            Image shade = backdrop.GetComponent<Image>();
            shade.color = new Color(0.04f, 0.025f, 0.02f, 0.72f);
            exitConfirm = backdrop;

            RectTransform confirm =
                Instantiate(dialogTemplate.gameObject, backdropRect).transform as RectTransform;
            confirm.name = "Dialog";
            confirm.gameObject.SetActive(true);
            SetAnchors(confirm, 0.24f, 0.34f, 0.76f, 0.66f);

            TMP_Text message = confirm.GetComponentInChildren<TMP_Text>(true);
            if (message != null)
            {
                RectTransform messageRect = message.rectTransform;
                SetAnchors(messageRect, 0.06f, 0.42f, 0.94f, 0.94f);
                message.text = exitConfirmMessage;
                message.textWrappingMode = TextWrappingModes.Normal;
                message.overflowMode = TextOverflowModes.Overflow;
                message.maxVisibleLines = 99;
                message.enableAutoSizing = true;
                message.fontSizeMin = 12f;
                message.fontSizeMax = 34f;
                message.alignment = TextAlignmentOptions.Center;
            }

            BuildConfirmButton(buttonTemplate, confirm, "Exit_Yes", exitConfirmYes, 0.08f, 0.46f, ConfirmExit);
            BuildConfirmButton(buttonTemplate, confirm, "Exit_No", exitConfirmNo, 0.54f, 0.92f, HideExitConfirm);

            backdrop.SetActive(false);
        }

        private void BuildConfirmButton(
            RectTransform template,
            RectTransform parent,
            string name,
            string label,
            float minX,
            float maxX,
            UnityEngine.Events.UnityAction action)
        {
            RectTransform button = Instantiate(template.gameObject, parent).transform as RectTransform;
            button.name = name;
            SetAnchors(button, minX, 0.08f, maxX, 0.32f);
            SetLabel(button, label);

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                // Without this a short label like "KHÔNG" wraps to one letter per line.
                StretchToParent(text.rectTransform);
                UIRuntime.ConfigureSingleLineListText(text, 28f);
                text.alignment = TextAlignmentOptions.Center;
            }

            UIRuntime.EnsureButton(button, action, selectedButtonSprite, true);
        }

        /// <summary>
        /// Sizes a rect purely from anchors. The authored footer buttons are anchor-stretched with a
        /// small sizeDelta, so treating that delta as a size collapses them to a few pixels.
        /// </summary>
        private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void ShowExitConfirm()
        {
            if (exitConfirm == null)
            {
                // No authored dialog to clone — do not strand the player with a dead button.
                ConfirmExit();
                return;
            }

            exitConfirm.SetActive(true);
            exitConfirm.transform.SetAsLastSibling();
            Sfx.Play(SfxId.UiConfirm);
        }

        private void HideExitConfirm()
        {
            if (exitConfirm != null && exitConfirm.activeSelf)
            {
                exitConfirm.SetActive(false);
                Sfx.Play(SfxId.UiDecline);
            }
        }

        private void ConfirmExit()
        {
            // The overlay zeroes timeScale while open and only restores it in its close routine,
            // which a scene load skips — so the main menu would come up frozen.
            Time.timeScale = 1f;
            SaveManager.Instance.ReturnToMenu();
        }

        private static void SetLabel(Transform button, string text)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = text;
        }

        private static void StretchToParent(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
