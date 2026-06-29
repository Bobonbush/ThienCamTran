using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>
    /// In-game pause menu (doc image rId35): Esc pauses the game and shows
    /// Continue / Options / Quit to Menu. "Options" opens the shared <see cref="OptionsScreen"/>.
    ///
    /// Pausing uses Time.timeScale = 0; all menu UI relies on unscaled time so it still animates.
    /// </summary>
    public class PauseMenu : UIScreen
    {
        [SerializeField] private OptionsScreen optionsScreen;
        [Tooltip("Name of the main-menu scene to return to on 'Quit to Menu'.")]
        [SerializeField] private string mainMenuScene = "MainMenu";

        public bool IsPaused { get; private set; }

        private void Update()
        {
            // Toggle on Esc. When Options is open, Esc/back is handled there first.
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (optionsScreen != null && optionsScreen.IsOpen)
                {
                    optionsScreen.Back();
                    return;
                }

                if (IsPaused) Resume();
                else Pause();
            }
        }

        public void Pause()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            Open();
        }

        public void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            if (optionsScreen != null && optionsScreen.IsOpen) optionsScreen.Close();
            Close();
        }

        // ---- Button hooks (wire these to the Button onClick in the inspector) ------------------

        public void OnContinue() => Resume();

        public void OnOptions()
        {
            if (optionsScreen != null) optionsScreen.Open();
        }

        public void OnQuitToMenu()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            SceneManager.LoadScene(mainMenuScene);
        }
    }
}
