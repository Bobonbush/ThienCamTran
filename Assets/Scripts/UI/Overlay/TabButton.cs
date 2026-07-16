using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Controls one animated menu tab.
    ///
    /// Hover:
    /// - Shows the active appearance.
    /// - Does not select the panel.
    /// - Does not turn the book page.
    ///
    /// Click:
    /// - Invokes MenuTabController.
    /// - Selects the panel.
    /// - MenuTabController handles page flipping.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TabButton :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private GameObject selectedIndicator;
        [SerializeField] private Animator tabAnimator;

        [Header("Animator State Names")]
        [SerializeField] private string idleStateName;
        [SerializeField] private string openStateName;
        [SerializeField] private string activeStateName;
        private Action clickCallback;

        private bool isSelected;
        private bool isHovered;
        private bool isRevealed;
        private bool inputEnabled;

        private int idleStateHash;
        private int openStateHash;
        private int activeStateHash;

        private VisualState currentState = VisualState.None;

        private enum VisualState
        {
            None,
            Hidden,
            Opening,
            Closing,
            Resting,
            Active
        }

        private void Awake()
        {
            FindReferences();
            CacheHashes();

            if (tabAnimator != null)
            {
                tabAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                tabAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }

        private void Start()
        {
            // Animator.Update cannot be called safely from Awake because the
            // Animator's native Awake may not have completed yet.
            ResetVisual();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            FindReferences();
            CacheHashes();
        }
#endif

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);

            clickCallback = null;
        }

        private void FindReferences()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (tabAnimator == null)
                tabAnimator = GetComponent<Animator>();
        }

        private void CacheHashes()
        {
            idleStateHash = string.IsNullOrWhiteSpace(idleStateName)
                ? 0
                : Animator.StringToHash(idleStateName);

            openStateHash = string.IsNullOrWhiteSpace(openStateName)
                ? 0
                : Animator.StringToHash(openStateName);

            activeStateHash = string.IsNullOrWhiteSpace(activeStateName)
                ? 0
                : Animator.StringToHash(activeStateName);
        }

        public void Bind(Action onClick)
        {
            clickCallback = onClick;

            if (button == null)
                return;

            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            if (!inputEnabled || !isRevealed)
                return;

            clickCallback?.Invoke();
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;

            if (button != null)
                button.interactable = enabled;

            if (!enabled)
                isHovered = false;

            RefreshVisual();
        }

        public void ResetVisual()
        {
            isSelected = false;
            isHovered = false;
            isRevealed = false;

            SetIndicator(false);

            PlayState(
                idleStateHash,
                VisualState.Hidden,
                0f,
                forceRestart: true
            );
        }

        public void PlayOpeningAnimation()
        {
            isRevealed = true;
            isHovered = false;

            SetIndicator(false);

            PlayState(
                openStateHash,
                VisualState.Opening,
                0f,
                forceRestart: true
            );
        }

        /// <summary>
        /// Forces the tab to remain visible at the final frame of its
        /// opening animation.
        /// </summary>
        public void ShowResting()
        {
            isRevealed = true;
            isHovered = false;

            if (!isSelected)
                SetIndicator(false);

            PlayState(
                openStateHash,
                VisualState.Resting,
                1f,
                forceRestart: true
            );
        }

        public void PlayClosingAnimation()
        {
            isSelected = false;
            isHovered = false;

            SetIndicator(false);

            PlayState(
                openStateHash,
                VisualState.Closing,
                1f,
                forceRestart: true,
                playbackSpeed: -1f
            );
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;

            SetIndicator(selected);

            RefreshVisual();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!inputEnabled || !isRevealed)
                return;

            isHovered = true;
            RefreshVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            RefreshVisual();
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!inputEnabled || !isRevealed)
                return;

            isHovered = true;
            RefreshVisual();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isHovered = false;
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (!isRevealed)
            {
                PlayState(
                    idleStateHash,
                    VisualState.Hidden,
                    0f
                );

                return;
            }

            if (isSelected || isHovered)
            {
                PlayState(
                    activeStateHash,
                    VisualState.Active,
                    0f
                );

                return;
            }

            PlayState(
                openStateHash,
                VisualState.Resting,
                1f
            );
        }

        private void PlayState(
            int stateHash,
            VisualState visualState,
            float normalizedTime,
            bool forceRestart = false,
            float playbackSpeed = 1f
        )
        {
            if (tabAnimator == null || stateHash == 0)
                return;

            if (!forceRestart && currentState == visualState)
                return;

            tabAnimator.enabled = true;
            tabAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            tabAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            tabAnimator.speed = 1f;

            if (!tabAnimator.HasState(0, stateHash))
            {
                Debug.LogWarning(
                    $"Animator state '{GetStateName(visualState)}' " +
                    $"was not found on tab '{name}'.",
                    this
                );

                return;
            }

            tabAnimator.Play(stateHash, 0, normalizedTime);
            tabAnimator.Update(0f);
            tabAnimator.speed = playbackSpeed;

            currentState = visualState;
        }

        private string GetStateName(VisualState visualState)
        {
            return visualState switch
            {
                VisualState.Hidden => idleStateName,
                VisualState.Opening => openStateName,
                VisualState.Closing => openStateName,
                VisualState.Resting => openStateName,
                VisualState.Active => activeStateName,
                _ => "Unknown"
            };
        }

        private void SetIndicator(bool value)
        {
            if (selectedIndicator != null)
                selectedIndicator.SetActive(value);
        }

    }
}
