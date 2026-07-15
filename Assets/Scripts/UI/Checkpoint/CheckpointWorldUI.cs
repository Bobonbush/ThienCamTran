using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CheckpointWorldUI : MonoBehaviour
{
    [Header("Visibility")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform contentRoot;

    [Header("Options")]
    [SerializeField] private Button restButton;
    [SerializeField] private Button statusButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite unselectedSprite;
    [SerializeField, Min(0f)] private float selectedIndent = 10f;

    [Header("Saving notification")]
    [SerializeField] private GameObject savingNotification;
    [SerializeField] private TMP_Text savingText;
    [SerializeField, Min(0.1f)] private float dotInterval = 0.35f;
    [SerializeField, Min(0f)] private float minimumSavingDisplayTime = 1.05f;

    private Button[] buttons;
    private Vector2[] baseButtonPositions;
    private SaveZone owner;
    private PlayerController player;
    private Coroutine visibilityRoutine;
    private Coroutine savingTextRoutine;
    private int selectedIndex;
    private float acceptInputAt;
    private bool isOpen;
    private bool saveCompleted;
    private Vector3 authoredLocalPosition;
    private Vector3 shownScale;
    private Vector3 hiddenScale;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (contentRoot == null)
            contentRoot = transform as RectTransform;

        // Preserve scene/prefab overrides made in the Inspector. Runtime only
        // mirrors X to the opposite side of the player; it must not replace Y.
        authoredLocalPosition = contentRoot.localPosition;
        shownScale = contentRoot.localScale;
        hiddenScale = shownScale * 0.92f;

        buttons = new[] { restButton, statusButton, cancelButton };
        baseButtonPositions = new Vector2[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                baseButtonPositions[i] = ((RectTransform)buttons[i].transform).anchoredPosition;
        }

        restButton?.onClick.AddListener(OnRestPressed);
        statusButton?.onClick.AddListener(OnStatusPressed);
        cancelButton?.onClick.AddListener(OnCancelPressed);

        SetVisibleImmediate(false);
    }

    private void Update()
    {
        if (!isOpen || Time.unscaledTime < acceptInputAt)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
            Select(Mathf.Max(0, selectedIndex - 1));
        else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
            Select(Mathf.Min(buttons.Length - 1, selectedIndex + 1));

        if (keyboard.qKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
        {
            OnCancelPressed();
            return;
        }

        if (keyboard.eKey.wasPressedThisFrame)
            buttons[selectedIndex]?.onClick.Invoke();
    }

    public void Open(SaveZone saveZone, PlayerController activePlayer)
    {
        owner = saveZone;
        player = activePlayer;

        bool playerIsOnLeft = activePlayer != null &&
                              activePlayer.transform.position.x < saveZone.transform.position.x;
        Open(playerIsOnLeft);
    }

    // Kept public so CheckpointTest can preview both placement directions.
    public void Open(bool playerIsOnLeftSide)
    {
        Vector3 targetPosition = authoredLocalPosition;
        targetPosition.x = Mathf.Abs(targetPosition.x) * (playerIsOnLeftSide ? 1f : -1f);
        contentRoot.localPosition = targetPosition;

        isOpen = true;
        acceptInputAt = Time.unscaledTime + 0.15f;
        Select(0);
        StartSavingText();
        AnimateVisibility(true);
    }

    public void NotifySaveCompleted()
    {
        saveCompleted = true;
    }

    public void Close()
    {
        if (owner != null)
            owner.CancelFromUI();
        else
            Hide();
    }

    public void Hide()
    {
        if (!isOpen && canvasGroup != null && canvasGroup.alpha <= 0f)
            return;

        isOpen = false;
        StopSavingText();
        AnimateVisibility(false);

        if (EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public bool IsOpenLegacy()
    {
        return isOpen;
    }

    private void Select(int index)
    {
        if (buttons == null || buttons.Length == 0)
            return;

        selectedIndex = Mathf.Clamp(index, 0, buttons.Length - 1);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            bool selected = i == selectedIndex;
            Image image = button.targetGraphic as Image;
            if (image != null)
                image.sprite = selected ? selectedSprite : unselectedSprite;

            RectTransform rect = (RectTransform)button.transform;
            Vector2 position = baseButtonPositions[i];
            if (selected)
                position.x += selectedIndent;
            rect.anchoredPosition = position;
        }

        if (EventSystem.current != null && buttons[selectedIndex] != null)
            EventSystem.current.SetSelectedGameObject(buttons[selectedIndex].gameObject);
    }

    private void OnRestPressed()
    {
        if (!isOpen)
            return;

        if (owner != null)
            owner.RestFromUI(player);
        else
            Hide();
    }

    private void OnStatusPressed()
    {
        if (!isOpen)
            return;

        if (owner != null)
            owner.StatusFromUI();
        else
            Hide();
    }

    private void OnCancelPressed()
    {
        if (!isOpen)
            return;

        Close();
    }

    private void StartSavingText()
    {
        StopSavingText();
        saveCompleted = false;
        if (savingNotification != null)
            savingNotification.SetActive(true);
        savingTextRoutine = StartCoroutine(AnimateSavingText());
    }

    private void StopSavingText()
    {
        if (savingTextRoutine != null)
        {
            StopCoroutine(savingTextRoutine);
            savingTextRoutine = null;
        }

        if (savingNotification != null)
            savingNotification.SetActive(false);
    }

    private IEnumerator AnimateSavingText()
    {
        int dots = 1;
        float elapsed = 0f;

        while (isOpen && (!saveCompleted || elapsed < minimumSavingDisplayTime))
        {
            if (savingText != null)
                savingText.text = "Đang lưu tiến trình" + new string('.', dots);

            dots = dots % 3 + 1;
            yield return new WaitForSecondsRealtime(dotInterval);
            elapsed += dotInterval;
        }

        if (isOpen && savingText != null)
            savingText.text = "Đã lưu xong";

        savingTextRoutine = null;
    }

    private void AnimateVisibility(bool visible)
    {
        if (visibilityRoutine != null)
            StopCoroutine(visibilityRoutine);
        visibilityRoutine = StartCoroutine(VisibilityRoutine(visible));
    }

    private IEnumerator VisibilityRoutine(bool visible)
    {
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;

        float startAlpha = canvasGroup.alpha;
        float targetAlpha = visible ? 1f : 0f;
        Vector3 startScale = contentRoot.localScale;
        Vector3 targetScale = visible ? shownScale : hiddenScale;
        float elapsed = 0f;
        const float duration = 0.16f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            contentRoot.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        contentRoot.localScale = targetScale;
        visibilityRoutine = null;
    }

    private void SetVisibleImmediate(bool visible)
    {
        isOpen = visible;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
        contentRoot.localScale = visible ? shownScale : hiddenScale;
        if (savingNotification != null)
            savingNotification.SetActive(false);
    }
}
