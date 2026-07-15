using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Main-menu title screen (doc image rId39). Per the design doc, trimmed to
    /// Start Game / Options / Quit Game (Achievements and Extras removed).
    /// "Options" reuses the very same <see cref="OptionsScreen"/> component as the pause menu.
    /// "Start Game" opens the placeholder <see cref="SaveSlotSelect"/> (doc image rId40).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject titleRoot;        // logo + Start/Options/Quit buttons
        [SerializeField] private SaveSlotSelect saveSlotSelect;
        [SerializeField] private OptionsScreen optionsScreen;

        private void Start()
        {
            ShowTitle();
            if (optionsScreen != null) optionsScreen.Closed += ShowTitle;
        }

        private void OnDestroy()
        {
            if (optionsScreen != null) optionsScreen.Closed -= ShowTitle;
        }

        private void ShowTitle()
        {
            if (titleRoot != null) titleRoot.SetActive(true);
        }

        private void HideTitle()
        {
            if (titleRoot != null) titleRoot.SetActive(false);
        }

        // ---- Button hooks ----------------------------------------------------------------------

        public void OnStartGame()
        {
            Sfx.Play(SfxId.UiConfirm);
            HideTitle();
            if (saveSlotSelect != null) saveSlotSelect.Open(onBack: ShowTitle);
        }

        public void OnOptions()
        {
            Sfx.Play(SfxId.UiConfirm);
            HideTitle();
            if (optionsScreen != null) optionsScreen.Open();
        }

        public void OnQuitGame()
        {
            Sfx.Play(SfxId.UiDecline);
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
