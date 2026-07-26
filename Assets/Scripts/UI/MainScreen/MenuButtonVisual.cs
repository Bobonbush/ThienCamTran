using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Sprite-swap state for the project's unselected/selected button artwork.</summary>
    public sealed class MenuButtonVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private Image image;
        private Sprite normal;
        private Sprite selected;
        private bool hovered;
        private bool keyboardSelected;
        private bool pressed;
        private bool latched;

        public void Configure(Image target, Sprite normalSprite, Sprite selectedSprite)
        {
            image = target;
            normal = normalSprite;
            selected = selectedSprite;
            Refresh();
        }

        /// <summary>
        /// Keeps a choice visibly selected, for persistent choices such as the active game mode.
        /// Momentary action buttons should leave this false.
        /// </summary>
        public void SetLatched(bool value)
        {
            latched = value;
            Refresh();
        }

        public void OnPointerEnter(PointerEventData eventData) { hovered = true; Refresh(); }
        public void OnPointerExit(PointerEventData eventData) { hovered = false; Refresh(); }
        public void OnPointerDown(PointerEventData eventData) { pressed = true; Refresh(); }
        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;

            // A pointer click selects a Unity Button in the EventSystem. That selection otherwise
            // remains indefinitely and leaves a momentary button displaying its selected sprite.
            keyboardSelected = false;
            if (!latched && EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == gameObject)
                EventSystem.current.SetSelectedGameObject(null);

            Refresh();
        }
        public void OnSelect(BaseEventData eventData) { keyboardSelected = true; Refresh(); }
        public void OnDeselect(BaseEventData eventData) { keyboardSelected = false; Refresh(); }

        private void Refresh()
        {
            if (image == null) return;
            bool showSelected = latched || pressed || hovered || keyboardSelected;
            image.sprite = showSelected && selected != null ? selected : normal;
        }
    }
}
