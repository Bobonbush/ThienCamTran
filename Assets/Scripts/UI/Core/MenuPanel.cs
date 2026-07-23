using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Contract for a tab inside the in-game overlay (Status / Map / Inventory / Database).
    /// The Slider tab bar (<see cref="MenuTabController"/>) owns the tabs and drives them through
    /// this interface; Nghi's Status / Inventory / Database panels implement it.
    ///
    /// Keep this stable — it is the shared boundary between the shell (Uyên) and the panels (Nghi).
    /// </summary>
    public abstract class MenuPanel : MonoBehaviour
    {
        /// <summary>Title shown on the tab bar for this panel (e.g. "Status", "Database").</summary>
        public abstract string TabLabel { get; }

        /// <summary>Called when this tab becomes the active one.</summary>
        public virtual void OnPanelOpened() { }

        /// <summary>Called when the overlay switches away from this tab (or the overlay closes).</summary>
        public virtual void OnPanelClosed() { }

        /// <summary>
        /// Forwarded directional input while this panel is active (arrow keys / WASD).
        /// Lets a panel move its internal selection without each panel re-reading the Input System.
        /// </summary>
        public virtual void OnNavigate(Vector2 direction) { }

        /// <summary>Forwarded "use / confirm" press (E) while this panel is active.</summary>
        public virtual void OnSubmit() { }

        /// <summary>Secondary action used by panels (Space by default).</summary>
        public virtual void OnAlternate() { }

        /// <summary>Mouse-wheel input forwarded by the overlay while this panel is active.</summary>
        public virtual void OnScroll(float delta, Vector2 screenPosition) { }

        /// <summary>
        /// Show/hide the panel root. Default toggles the GameObject; override if a panel needs
        /// to keep running while hidden.
        /// </summary>
        public virtual void SetActivePanel(bool active)
        {
            gameObject.SetActive(active);
        }
    }
}
