using System.Collections;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Base class for any full-screen menu surface (pause menu, options, main menu...).
    /// Wraps a CanvasGroup so screens can fade in/out and be toggled without enabling/disabling
    /// the GameObject (which would kill running coroutines). Background-agnostic on purpose:
    /// the same screen prefab can sit on the in-game pause canvas or the main-menu canvas.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIScreen : MonoBehaviour
    {
        [SerializeField] private float fadeDuration = 0.15f;

        private CanvasGroup canvasGroup;
        private Coroutine fadeRoutine;

        public bool IsOpen { get; private set; }

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            // Start hidden but keep the component active so coroutines can run.
            SetVisible(false, instant: true);
        }

        public virtual void Open()
        {
            IsOpen = true;
            SetVisible(true);
            OnOpened();
        }

        public virtual void Close()
        {
            IsOpen = false;
            SetVisible(false);
            OnClosed();
        }

        protected virtual void OnOpened() { }
        protected virtual void OnClosed() { }

        private void SetVisible(bool visible, bool instant = false)
        {
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;

            if (instant || fadeDuration <= 0f)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                return;
            }

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(Fade(visible ? 1f : 0f));
        }

        private IEnumerator Fade(float target)
        {
            float speed = 1f / fadeDuration;
            // Use unscaled time so menus still fade while the game is paused (timeScale = 0).
            while (!Mathf.Approximately(canvasGroup.alpha, target))
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, speed * Time.unscaledDeltaTime);
                yield return null;
            }
            fadeRoutine = null;
        }
    }
}
