using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.UI
{
    /// <summary>
    /// In-game overlay "Slider" / tab bar (doc lines 369–377): Status — Map — Inventory — Database.
    /// Opens with I, closes with Q, switches tabs with '[' and ']'. The Map tab is intentionally
    /// left empty for now (the author fills it later).
    ///
    /// Owns the tabs and drives the active <see cref="MenuPanel"/> through its contract, so Nghi's
    /// Status / Inventory / Database panels only implement <see cref="MenuPanel"/> and never touch
    /// tab-switching logic. Pass an empty (placeholder) panel for Map.
    /// </summary>
    public class MenuTabController : UIScreen
    {
        [Tooltip("Tabs in display order. Map can be an empty placeholder MenuPanel.")]
        [SerializeField] private MenuPanel[] panels;
        [Tooltip("Optional tab-bar labels/highlights, one per panel, kept in sync with selection.")]
        [SerializeField] private TabButton[] tabButtons;

        private int activeIndex = -1;

        protected override void Awake()
        {
            base.Awake();
            // Hide all panels initially.
            foreach (var p in panels)
                if (p != null) p.SetActivePanel(false);

            if (tabButtons != null)
                for (int i = 0; i < tabButtons.Length; i++)
                {
                    int idx = i;
                    if (tabButtons[i] != null) tabButtons[i].Bind(() => SelectTab(idx));
                }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // Toggle the whole overlay with I; close with Q when open.
            if (kb.iKey.wasPressedThisFrame)
            {
                if (IsOpen) CloseOverlay();
                else OpenOverlay();
                return;
            }
            if (!IsOpen) return;

            if (kb.qKey.wasPressedThisFrame) { CloseOverlay(); return; }

            // Tab switching with [ and ] (mirrors the Player map's Previous/Next bindings).
            if (kb.leftBracketKey.wasPressedThisFrame) Step(-1);
            else if (kb.rightBracketKey.wasPressedThisFrame) Step(+1);

            // Forward directional + submit input to the active panel.
            Vector2 nav = ReadNavigation(kb);
            if (nav != Vector2.zero) ActivePanel?.OnNavigate(nav);
            if (kb.eKey.wasPressedThisFrame) ActivePanel?.OnSubmit();
        }

        private MenuPanel ActivePanel =>
            (activeIndex >= 0 && activeIndex < panels.Length) ? panels[activeIndex] : null;

        public void OpenOverlay()
        {
            Open();
            Time.timeScale = 0f; // pause world while browsing menus
            SelectTab(Mathf.Max(0, activeIndex));
        }

        public void CloseOverlay()
        {
            ActivePanel?.OnPanelClosed();
            foreach (var p in panels)
                if (p != null) p.SetActivePanel(false);
            activeIndex = -1;
            Time.timeScale = 1f;
            Close();
        }

        private void Step(int dir)
        {
            if (panels.Length == 0) return;
            int next = (activeIndex + dir + panels.Length) % panels.Length;
            SelectTab(next);
        }

        public void SelectTab(int index)
        {
            if (index < 0 || index >= panels.Length || index == activeIndex) return;

            if (ActivePanel != null)
            {
                ActivePanel.OnPanelClosed();
                ActivePanel.SetActivePanel(false);
            }

            activeIndex = index;

            if (ActivePanel != null)
            {
                ActivePanel.SetActivePanel(true);
                ActivePanel.OnPanelOpened();
            }

            if (tabButtons != null)
                for (int i = 0; i < tabButtons.Length; i++)
                    if (tabButtons[i] != null) tabButtons[i].SetSelected(i == index);
        }

        private static Vector2 ReadNavigation(Keyboard kb)
        {
            float x = 0f, y = 0f;
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) x -= 1f;
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) x += 1f;
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) y += 1f;
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) y -= 1f;
            return new Vector2(x, y);
        }
    }
}
