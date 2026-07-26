using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Controls the animated book menu.
    ///
    /// I              = open/close
    /// Q              = close
    /// [ / Page Up    = previous tab
    /// ] / Page Down  = next tab
    ///
    /// Panels and tabButtons must use the same ordering.
    /// </summary>
    public class MenuTabController : UIScreen
    {
        [Header("UI Structure")]
        [Tooltip("Panels in tab order.")]
        [SerializeField] private MenuPanel[] panels;

        [Tooltip("Tab buttons in exactly the same order as Panels.")]
        [SerializeField] private TabButton[] tabButtons;

        [Header("Default Tab")]
        [Tooltip("Inventory tab index. Used only before any tab has been selected.")]
        [SerializeField, Min(0)] private int defaultTabIndex = 0;

        [Header("Book Animator")]
        [SerializeField] private Animator bookAnimator;

        [Tooltip("Duration of the book-opening animation.")]
        [SerializeField, Min(0f)] private float openingSequenceDuration = 1.9f;

        [Tooltip("Duration of the book-closing animation.")]
        [SerializeField, Min(0f)] private float closingSequenceDuration = 1.9f;

        [Header("Tab Reveal")]
        [Tooltip("Delay between each tab popping out.")]
        [SerializeField, Min(0f)] private float tabRevealStagger = 0.06f;

        [Tooltip("Duration of one tab's pop-out animation.")]
        [SerializeField, Min(0f)] private float tabOpenAnimationDuration = 0.42f;

        [Tooltip("Delay between each tab retracting when the menu closes.")]
        [SerializeField, Min(0f)] private float tabCloseStagger = 0.06f;

        [Tooltip("Duration of one tab's retract animation.")]
        [SerializeField, Min(0f)] private float tabCloseAnimationDuration = 0.52f;

        [Header("Page Navigation")]
        [Tooltip("Duration of one page-flip animation.")]
        [SerializeField, Min(0f)] private float pageFlipDuration = 0.8f;

        private int activeIndex = -1;
        private int lastSelectedIndex = -1;
        private int requestedTabIndex = -1;

        private bool isTransitioning;
        private bool initialized;
        private Coroutine transitionRoutine;
        private TMP_Text navigationGuide;

        private static readonly int OpenHash =
            Animator.StringToHash("Book_Cover_Open");

        private static readonly int CloseHash =
            Animator.StringToHash("Book_Cover_Close");

        private static readonly int FlipLeftHash =
            Animator.StringToHash("Book_Flip_Left");

        private static readonly int FlipRightHash =
            Animator.StringToHash("Book_Flip_Right");

        private MenuPanel ActivePanel
        {
            get
            {
                if (panels == null ||
                    activeIndex < 0 ||
                    activeIndex >= panels.Length)
                {
                    return null;
                }

                return panels[activeIndex];
            }
        }

        protected override void Awake()
        {
            base.Awake();

            panels ??= Array.Empty<MenuPanel>();
            tabButtons ??= Array.Empty<TabButton>();

            if (bookAnimator == null)
                bookAnimator = GetComponentInChildren<Animator>(true);

            if (bookAnimator != null)
            {
                bookAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                bookAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            defaultTabIndex = GetValidDefaultIndex();

            CreateNavigationGuide();
            HideAllPanels();
        }

        private void Start()
        {
            EnsureInitialized();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            openingSequenceDuration = Mathf.Max(0f, openingSequenceDuration);
            closingSequenceDuration = Mathf.Max(0f, closingSequenceDuration);
            tabRevealStagger = Mathf.Max(0f, tabRevealStagger);
            tabOpenAnimationDuration = Mathf.Max(0f, tabOpenAnimationDuration);
            tabCloseStagger = Mathf.Max(0f, tabCloseStagger);
            tabCloseAnimationDuration = Mathf.Max(0f, tabCloseAnimationDuration);
            pageFlipDuration = Mathf.Max(0f, pageFlipDuration);

            if (panels != null && panels.Length > 0)
                defaultTabIndex = Mathf.Clamp(defaultTabIndex, 0, panels.Length - 1);
            else
                defaultTabIndex = 0;
        }
#endif

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return;

            if (ItemObtained.IsShowing && !IsOpen)
                return;

            if (InputManager.Instance.Controls.Player.Inventory.WasPressedThisFrame())
            {
                if (IsOpen)
                    CloseOverlay();
                else
                    OpenOverlay();

                return;
            }

            if (!IsOpen || isTransitioning)
                return;

            if (InputManager.Instance.Controls.Player.OnDeny.WasPressedThisFrame())
            {
                CloseOverlay();
                return;
            }

            if (InputManager.Instance.Controls.Player.PreviousPage.WasPressedThisFrame())
            {
                Step(-1);
            }
            else if (InputManager.Instance.Controls.Player.NextPage.WasPressedThisFrame())
            {
                Step(1);
            }
            

            Vector2 navigation = ReadNavigation(keyboard);

            if (navigation != Vector2.zero)
                ActivePanel?.OnNavigate(navigation);

            if (InputManager.Instance.Controls.Player.Interact.WasPressedThisFrame())
                ActivePanel?.OnSubmit();

            if (InputManager.Instance.Controls.Player.SlotSwap.WasPressedThisFrame())
                ActivePanel?.OnAlternate();

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                    TrySelectTabAt(mouse.position.ReadValue());

                float wheel = mouse.scroll.ReadValue().y;
                if (!Mathf.Approximately(wheel, 0f))
                    ActivePanel?.OnScroll(wheel, mouse.position.ReadValue());
            }
        }

        private void TrySelectTabAt(Vector2 screenPosition)
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                TabButton tabButton = tabButtons[i];
                RectTransform rect = tabButton != null ? tabButton.transform as RectTransform : null;
                if (rect == null || !RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition))
                    continue;

                OnTabClicked(i);
                return;
            }
        }

        private void InitializeTabButtons()
        {
            if (panels.Length != tabButtons.Length)
            {
                Debug.LogWarning(
                    $"{nameof(MenuTabController)} on '{name}' has " +
                    $"{panels.Length} panels but {tabButtons.Length} tab buttons. " +
                    "Both arrays should have the same length and order.",
                    this
                );
            }

            for (int i = 0; i < tabButtons.Length; i++)
            {
                TabButton tabButton = tabButtons[i];

                if (tabButton == null)
                    continue;

                int capturedIndex = i;

                tabButton.Bind(() => OnTabClicked(capturedIndex));
                tabButton.SetInputEnabled(false);
                tabButton.ResetVisual();
            }
        }

        private void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;
            InitializeTabButtons();
        }

        public void OpenOverlay()
        {
            if (IsOpen || isTransitioning)
                return;

            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                Debug.LogError("MenuOverlay cannot open while its controller is inactive.", this);
                return;
            }

            EnsureInitialized();

            StopTransitionRoutine();
            Sfx.Play(SfxId.UiOpen);
            Open();
            transitionRoutine = StartCoroutine(OpenRoutine());
            Time.timeScale = 0f;
        }

        public void OpenOverlayAtTab(string tabLabel)
        {
            requestedTabIndex = FindTabIndex(tabLabel);

            if (IsOpen && !isTransitioning)
            {
                if (requestedTabIndex >= 0)
                    SelectTab(requestedTabIndex);
                requestedTabIndex = -1;
                return;
            }

            OpenOverlay();
        }

        private IEnumerator OpenRoutine()
        {
            isTransitioning = true;
            activeIndex = -1;

            HideAllPanels();
            SetTabInputEnabled(false);

            foreach (TabButton tabButton in tabButtons)
            {
                if (tabButton != null)
                    tabButton.ResetVisual();
            }

            PlayBookState(OpenHash, resetAnimator: true);

            yield return new WaitForSecondsRealtime(openingSequenceDuration);

            // Reveal every tab.
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] != null)
                    tabButtons[i].PlayOpeningAnimation();

                if (tabRevealStagger > 0f &&
                    i < tabButtons.Length - 1)
                {
                    yield return new WaitForSecondsRealtime(tabRevealStagger);
                }
            }

            // Let the final tab finish its pop-out animation.
            if (tabOpenAnimationDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    tabOpenAnimationDuration
                );
            }

            // Explicitly put every tab into its visible resting state.
            // This fixes the problem where only the selected tab remains visible.
            foreach (TabButton tabButton in tabButtons)
            {
                if (tabButton != null)
                    tabButton.ShowResting();
            }

            int tabToOpen = requestedTabIndex >= 0
                ? requestedTabIndex
                : GetRestoredTabIndex();
            requestedTabIndex = -1;

            if (tabToOpen >= 0)
                SelectTabImmediate(tabToOpen);

            SetTabInputEnabled(true);

            isTransitioning = false;
            transitionRoutine = null;
        }

        public void CloseOverlay()
        {
            if (!IsOpen || isTransitioning)
                return;
            Sfx.Play(SfxId.UiClose);
            StopTransitionRoutine();
            transitionRoutine = StartCoroutine(CloseRoutine());
        }

        private IEnumerator CloseRoutine()
        {
            isTransitioning = true;
            SetTabInputEnabled(false);

            if (activeIndex >= 0)
                lastSelectedIndex = activeIndex;

            ActivePanel?.OnPanelClosed();
            HideAllPanels();

            activeIndex = -1;

            // Retract every tab before closing the book cover.
            for (int i = tabButtons.Length - 1; i >= 0; i--)
            {
                if (tabButtons[i] != null)
                    tabButtons[i].PlayClosingAnimation();

                if (tabCloseStagger > 0f && i > 0)
                    yield return new WaitForSecondsRealtime(tabCloseStagger);
            }

            if (tabCloseAnimationDuration > 0f)
                yield return new WaitForSecondsRealtime(tabCloseAnimationDuration);

            foreach (TabButton tabButton in tabButtons)
            {
                if (tabButton != null)
                    tabButton.ResetVisual();
            }

            // Some older versions of the controller do not contain the close
            // state. Reversing the open state keeps the menu functional while
            // still preferring the dedicated close animation when available.
            bool usedDedicatedCloseState = PlayBookState(
                CloseHash,
                warnIfMissing: false
            );

            if (!usedDedicatedCloseState)
            {
                PlayBookState(
                    OpenHash,
                    normalizedTime: 1f,
                    playbackSpeed: -1f
                );
            }

            float bookCloseDuration = usedDedicatedCloseState
                ? closingSequenceDuration
                : openingSequenceDuration;

            yield return new WaitForSecondsRealtime(bookCloseDuration);

            Close();
            Time.timeScale = 1f;

            isTransitioning = false;
            transitionRoutine = null;
        }

        private void OnTabClicked(int index)
        {
            if (!IsOpen || isTransitioning)
                return;

            SelectTab(index);
        }

        private void Step(int direction)
        {
            if (panels == null || panels.Length == 0)
                return;

            int currentIndex = activeIndex;

            if (currentIndex < 0)
                currentIndex = GetRestoredTabIndex();

            int nextIndex = Mathf.Clamp(
                currentIndex + direction,
                0,
                panels.Length - 1
            );

            SelectTab(nextIndex);
        }

        /// <summary>
        /// Selects a tab and plays a page flip when moving between tabs.
        /// Hovering does not call this method.
        /// </summary>
        public void SelectTab(int index)
        {
            if (!IsOpen || isTransitioning)
                return;

            if (!IsValidTabIndex(index) || index == activeIndex)
                return;

            if (activeIndex < 0)
            {
                SelectTabImmediate(index);
                return;
            }

            transitionRoutine = StartCoroutine(NavigateRoutine(index));
        }

        private IEnumerator NavigateRoutine(int targetIndex)
        {
            isTransitioning = true;
            SetTabInputEnabled(false);

            MenuPanel previousPanel = ActivePanel;

            if (previousPanel != null)
            {
                previousPanel.OnPanelClosed();
                previousPanel.SetActivePanel(false);
            }

            int direction = targetIndex > activeIndex ? 1 : -1;
            int flipHash = direction > 0 ? FlipLeftHash : FlipRightHash;

            while (activeIndex != targetIndex)
            {
                PlayBookState(flipHash);

                if (pageFlipDuration > 0f)
                    yield return new WaitForSecondsRealtime(pageFlipDuration);

                activeIndex += direction;
                UpdateSelectedTabs();
            }

            lastSelectedIndex = activeIndex;

            MenuPanel selectedPanel = ActivePanel;

            if (selectedPanel != null)
            {
                selectedPanel.SetActivePanel(true);
                selectedPanel.OnPanelOpened();
            }

            SetTabInputEnabled(true);
            isTransitioning = false;
            transitionRoutine = null;
        }

        private void SelectTabImmediate(int index)
        {
            if (!IsValidTabIndex(index))
                return;

            MenuPanel previousPanel = ActivePanel;

            if (previousPanel != null)
            {
                previousPanel.OnPanelClosed();
                previousPanel.SetActivePanel(false);
            }

            activeIndex = index;
            lastSelectedIndex = index;

            MenuPanel selectedPanel = ActivePanel;

            if (selectedPanel != null)
            {
                selectedPanel.SetActivePanel(true);
                selectedPanel.OnPanelOpened();
            }

            UpdateSelectedTabs();
        }

        private void CreateNavigationGuide()
        {
            Transform existing = transform.Find("Runtime_NavigationGuide");
            if (existing != null)
            {
                navigationGuide = existing.GetComponentInChildren<TMP_Text>(true);
                return;
            }

            TMP_FontAsset font = GetComponentInChildren<TMP_Text>(true)?.font;
            GameObject panel = new GameObject("Runtime_NavigationGuide", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            RectTransform panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = new Vector2(0.16f, 0.015f);
            panelRect.anchorMax = new Vector2(0.84f, 0.065f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            Image background = panel.GetComponent<Image>();
            background.color = new Color(0.04f, 0.025f, 0.02f, 0.82f);
            background.raycastTarget = false;

            GameObject label = new GameObject("Guide", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(panel.transform, false);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 2f);
            labelRect.offsetMax = new Vector2(-10f, -2f);
            navigationGuide = label.GetComponent<TMP_Text>();
            navigationGuide.font = font;
            navigationGuide.text = "Click tabs / items  •  Tab or [ ]: page  •  WASD / Arrows: select  •  E: use / equip  •  Space: ring slot  •  Wheel: scroll  •  I / Q: close";
            navigationGuide.alignment = TextAlignmentOptions.Center;
            navigationGuide.enableAutoSizing = true;
            navigationGuide.fontSizeMin = 10f;
            navigationGuide.fontSizeMax = 18f;
            navigationGuide.textWrappingMode = TextWrappingModes.NoWrap;
            navigationGuide.overflowMode = TextOverflowModes.Ellipsis;
            navigationGuide.raycastTarget = false;
            panel.transform.SetAsLastSibling();
        }

        private void UpdateSelectedTabs()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] != null)
                    tabButtons[i].SetSelected(i == activeIndex);
            }
        }

        private int GetValidDefaultIndex()
        {
            if (panels == null || panels.Length == 0)
                return -1;

            return Mathf.Clamp(defaultTabIndex, 0, panels.Length - 1);
        }

        private int GetRestoredTabIndex()
        {
            if (panels == null || panels.Length == 0)
                return -1;

            if (lastSelectedIndex >= 0 &&
                lastSelectedIndex < panels.Length)
            {
                return lastSelectedIndex;
            }

            return GetValidDefaultIndex();
        }

        private bool IsValidTabIndex(int index)
        {
            return panels != null &&
                   index >= 0 &&
                   index < panels.Length;
        }

        private int FindTabIndex(string tabLabel)
        {
            if (panels == null || string.IsNullOrWhiteSpace(tabLabel))
                return -1;

            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null && string.Equals(
                    panels[i].TabLabel,
                    tabLabel,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            Debug.LogWarning($"Menu tab '{tabLabel}' was not found.", this);
            return -1;
        }

        private void HideAllPanels()
        {
            if (panels == null)
                return;

            foreach (MenuPanel panel in panels)
            {
                if (panel != null)
                    panel.SetActivePanel(false);
            }
        }

        private void SetTabInputEnabled(bool enabled)
        {
            foreach (TabButton tabButton in tabButtons)
            {
                if (tabButton != null)
                    tabButton.SetInputEnabled(enabled);
            }
        }

        private bool PlayBookState(
            int stateHash,
            bool resetAnimator = false,
            float normalizedTime = 0f,
            float playbackSpeed = 1f,
            bool warnIfMissing = true
        )
        {
            if (bookAnimator == null)
                return false;

            bookAnimator.enabled = true;
            bookAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            bookAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            bookAnimator.speed = 1f;

            if (resetAnimator)
            {
                bookAnimator.Rebind();
                bookAnimator.Update(0f);
            }

            if (!bookAnimator.HasState(0, stateHash))
            {
                if (warnIfMissing)
                {
                    Debug.LogWarning(
                        $"Animator state hash '{stateHash}' was not found " +
                        $"on '{bookAnimator.name}'.",
                        bookAnimator
                    );
                }

                return false;
            }

            bookAnimator.Play(stateHash, 0, normalizedTime);
            bookAnimator.Update(0f);
            bookAnimator.speed = playbackSpeed;

            return true;
        }

        private void StopTransitionRoutine()
        {
            if (transitionRoutine == null)
                return;

            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        private static Vector2 ReadNavigation(Keyboard keyboard)
        {
            float x = 0f;
            float y = 0f;

            if (InputManager.Instance.LeftPress)
            {
                x -= 1f;
            }

            if (InputManager.Instance.RightPress)
            {
                x += 1f;
            }

            if (InputManager.Instance.UpPress)
            {
                y += 1f;
            }

            if (InputManager.Instance.DownPress)
            {
                y -= 1f;
            }

            return new Vector2(x, y);
        }

        private void OnDestroy()
        {
            if (IsOpen)
                Time.timeScale = 1f;
        }

        private void OnDisable()
        {
            if (!IsOpen && !isTransitioning)
                return;

            StopTransitionRoutine();
            if (IsOpen)
                Close();
            Time.timeScale = 1f;
        }
    }
}
