using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Owns the authored MainMenuNew hierarchy. Save-slot and option changes intentionally remain
    /// UI-local; the events at the bottom are the future hand-off points for game services.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Serializable] public sealed class PlayRequestedEvent : UnityEvent<int, string, int> { }

        [Header("UI artwork")]
        [SerializeField] private Sprite selectedButtonSprite;
        [SerializeField] private Sprite[] selectableIcons = new Sprite[11];
        [SerializeField] private Sprite videoLabelSprite;
        [SerializeField] private Sprite audioLabelSprite;
        [SerializeField] private Sprite controlLabelSprite;

        [Header("Backend services")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private InputActionAsset inputActions;

        [Header("Frontend-only events")]
        [SerializeField] private PlayRequestedEvent playRequested = new PlayRequestedEvent();

        private GameObject closedBook;
        private GameObject openBook;
        private GameObject savePanelObject;
        private GameObject optionPanelObject;
        private MainMenuSaveSlotPanel savePanel;
        private MainMenuOptionsPanel optionPanel;

        public Sprite SelectedButtonSprite => selectedButtonSprite;
        public Sprite VideoLabelSprite => videoLabelSprite;
        public Sprite AudioLabelSprite => audioLabelSprite;
        public Sprite ControlLabelSprite => controlLabelSprite;
        public AudioMixer AudioMixer => audioMixer;
        public InputActionAsset InputActions => inputActions;
        public PlayRequestedEvent PlayRequested => playRequested;

        private void Awake()
        {
            UIRuntime.ConfigureCanvas(gameObject, true);
            SettingsService.EnsureExists(audioMixer);
            InputBindingService.EnsureExists(inputActions);

            closedBook = FindObject("Book_Close_Panel");
            openBook = FindObject("Book_Open_Panel");
            savePanelObject = FindObject("Save_Slot_Panel");
            optionPanelObject = FindObject("Option_Panel ");

            BindButton("StartButton", OpenSaveSlots);
            BindButton("OptionButton", OpenOptions);
            BindButton("QuitButton", QuitGame);
            BindButton("Back_Button", Back);

            if (savePanelObject != null)
            {
                savePanel = savePanelObject.GetComponent<MainMenuSaveSlotPanel>();
                if (savePanel == null) savePanel = savePanelObject.AddComponent<MainMenuSaveSlotPanel>();
                savePanel.Initialize(this, selectableIcons);
            }

            if (optionPanelObject != null)
            {
                optionPanel = optionPanelObject.GetComponent<MainMenuOptionsPanel>();
                if (optionPanel == null) optionPanel = optionPanelObject.AddComponent<MainMenuOptionsPanel>();
                optionPanel.Initialize(this);
            }

            ConfigureButtonVisuals(transform);
            ShowClosedBook();
        }

        public Button EnsureButton(Transform target, UnityAction action, bool forceSelectedVisual = false)
        {
            if (target == null) return null;
            Button button = target.GetComponent<Button>();
            if (button == null) button = target.gameObject.AddComponent<Button>();
            if (button.targetGraphic == null) button.targetGraphic = target.GetComponent<Graphic>();
            button.transition = Selectable.Transition.None;
            button.onClick.RemoveAllListeners();
            if (action != null) button.onClick.AddListener(action);
            ConfigureButtonVisual(button, forceSelectedVisual);
            return button;
        }

        public void NotifyPlayRequested(int slotIndex, string mode, int iconIndex)
        {
            Debug.Log($"[MainMenu] Play requested: slot {slotIndex + 1}, mode {mode}, icon {iconIndex}. Backend hand-off is intentionally not connected.");
            playRequested.Invoke(slotIndex, mode, iconIndex);
        }

        private void BindButton(string objectName, UnityAction action)
        {
            Transform target = UIRuntime.Find(transform, objectName);
            if (target != null) EnsureButton(target, action);
        }

        private void OpenSaveSlots()
        {
            Sfx.Play(SfxId.UiConfirm);
            SetActive(closedBook, false);
            SetActive(openBook, true);
            SetActive(optionPanelObject, false);
            SetActive(savePanelObject, true);
            savePanel?.Open();
        }

        private void OpenOptions()
        {
            Sfx.Play(SfxId.UiConfirm);
            SetActive(closedBook, false);
            SetActive(openBook, true);
            SetActive(savePanelObject, false);
            SetActive(optionPanelObject, true);
            optionPanel?.Open();
        }

        private void Back()
        {
            Sfx.Play(SfxId.UiDecline);
            if (savePanelObject != null && savePanelObject.activeSelf && savePanel != null && savePanel.Back())
                return;
            if (optionPanelObject != null && optionPanelObject.activeSelf)
                optionPanel?.CancelUncommitted();
            ShowClosedBook();
        }

        private void ShowClosedBook()
        {
            SetActive(closedBook, true);
            SetActive(openBook, false);
            SetActive(savePanelObject, false);
            SetActive(optionPanelObject, false);
        }

        private void QuitGame()
        {
            Sfx.Play(SfxId.UiDecline);
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private GameObject FindObject(string objectName)
        {
            Transform found = UIRuntime.Find(transform, objectName);
            return found != null ? found.gameObject : null;
        }

        private void ConfigureButtonVisuals(Transform root)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
                ConfigureButtonVisual(button);
        }

        public MenuButtonVisual ConfigureButtonVisual(Button button, bool force = false)
        {
            if (button == null || selectedButtonSprite == null) return null;
            Image image = button.targetGraphic as Image;
            if (image == null) image = button.GetComponent<Image>();
            if (image == null || image.sprite == null) return null;

            bool usesButtonArtwork =
                image.sprite.name.IndexOf("Unselected", StringComparison.OrdinalIgnoreCase) >= 0 ||
                image.sprite.name.IndexOf("Button_Medium", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!force && !usesButtonArtwork) return null;

            MenuButtonVisual visual = button.GetComponent<MenuButtonVisual>();
            if (visual == null) visual = button.gameObject.AddComponent<MenuButtonVisual>();
            visual.Configure(image, image.sprite, selectedButtonSprite);
            return visual;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null) target.SetActive(active);
        }
    }
}
