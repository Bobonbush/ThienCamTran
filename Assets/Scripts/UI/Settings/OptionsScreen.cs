using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Shared Options screen (doc images rId36 / rId41). Identical whether reached from the pause
    /// menu or the main menu — only the background behind it differs, so this component is
    /// background-agnostic and is meant to be reused by both. Per the design doc the required
    /// categories are Audio, Video and Keyboard.
    ///
    /// Shows the category list and swaps in one sub-panel at a time. "Back" either returns to the
    /// category list (if inside a sub-panel) or closes the whole screen (raising <see cref="Closed"/>).
    /// </summary>
    public class OptionsScreen : UIScreen
    {
        [Header("Category list")]
        [SerializeField] private GameObject categoryList;

        [Header("Sub-panels")]
        [SerializeField] private GameObject audioPanel;
        [SerializeField] private GameObject videoPanel;
        [SerializeField] private GameObject keyboardPanel;

        /// <summary>Raised when the screen fully closes (so the caller can refocus its own menu).</summary>
        public System.Action Closed;

        private GameObject activeSubPanel;

        protected override void OnOpened()
        {
            ShowCategoryList();
        }

        private void ShowCategoryList()
        {
            activeSubPanel = null;
            SetActive(audioPanel, false);
            SetActive(videoPanel, false);
            SetActive(keyboardPanel, false);
            SetActive(categoryList, true);
        }

        private void ShowSubPanel(GameObject panel)
        {
            SetActive(categoryList, false);
            SetActive(audioPanel, panel == audioPanel);
            SetActive(videoPanel, panel == videoPanel);
            SetActive(keyboardPanel, panel == keyboardPanel);
            activeSubPanel = panel;
        }

        // ---- Category buttons ------------------------------------------------------------------

        public void OnAudio() => ShowSubPanel(audioPanel);
        public void OnVideo() => ShowSubPanel(videoPanel);
        public void OnKeyboard() => ShowSubPanel(keyboardPanel);

        /// <summary>Back/cancel: drop from a sub-panel to the list, or close the screen.</summary>
        public void Back()
        {
            if (activeSubPanel != null)
            {
                ShowCategoryList();
            }
            else
            {
                Close();
                Closed?.Invoke();
            }
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
