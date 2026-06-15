using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// The "face" of the dialog — only handles DISPLAY.
// You will have two of these sharing this script:
//   - a rectangular box at the bottom  -> followTarget = false
//   - a speech bubble above a character -> followTarget = true
public class DialogView : MonoBehaviour
{
    [Header("UI References")]
    public GameObject root;               // Root object of this view (toggles the whole group)
    public TMP_Text speakerNameText;      // Can be left empty if the bubble style needs no name
    public TMP_Text dialogText;
    public Transform choicesContainer;
    public Button choiceButtonPrefab;

    [Header("Bubble style (follows a character)")]
    public bool followTarget = false;     // true = bubble follows the head; false = fixed box
    public Vector3 worldOffset = new Vector3(0f, 2f, 0f);  // how high above the head (world units)

    [Header("Open / Close animation")]
    public bool useFade = true;           // fade the alpha in/out
    public bool useSlide = true;          // slide horizontally (left -> right). Tick both = combined.
    public float animDuration = 0.25f;    // seconds
    public float slideDistance = 200f;    // how far to the left it starts, in pixels

    private Transform target;
    private readonly List<Button> spawnedButtons = new List<Button>();

    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Vector2 shownPos;              // resting anchoredPosition (set in the editor)
    private Vector2 slideOffset;           // current animation offset, in pixels
    private bool isOpen;
    private bool initialized;
    private Coroutine animRoutine;

    public void SetTarget(Transform t) => target = t;

    private void Awake()
    {
        Init();
        HideInstant();                     // start hidden, no flash
    }

    private void Init()
    {
        if (initialized) return;
        rect = root.GetComponent<RectTransform>();
        canvasGroup = root.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = root.AddComponent<CanvasGroup>();
        shownPos = rect.anchoredPosition;
        initialized = true;
    }

    public void Open()
    {
        Init();
        isOpen = true;

        // Set the starting state BEFORE showing so there is no 1-frame flash.
        canvasGroup.alpha = useFade ? 0f : 1f;
        slideOffset = useSlide ? new Vector2(-slideDistance, 0f) : Vector2.zero;
        root.SetActive(true);
        ApplyTransform();

        StartAnim(AnimateIn());
    }

    public void Close()
    {
        Init();
        if (!isOpen)                       // never opened -> just make sure it's hidden
        {
            HideInstant();
            return;
        }
        isOpen = false;
        StartAnim(AnimateOut());
    }

    public void SetText(string speaker, string body)
    {
        if (speakerNameText != null) speakerNameText.text = speaker;
        dialogText.text = body;
    }

    public void SpawnChoice(string label, UnityAction onClick)
    {
        Button button = Instantiate(choiceButtonPrefab, choicesContainer);
        button.GetComponentInChildren<TMP_Text>().text = label;
        button.onClick.AddListener(onClick);
        spawnedButtons.Add(button);
    }

    public void ClearChoices()
    {
        foreach (Button b in spawnedButtons)
            Destroy(b.gameObject);
        spawnedButtons.Clear();
    }

    private IEnumerator AnimateIn()
    {
        float t = 0f;
        while (animDuration > 0f && t < animDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / animDuration));
            canvasGroup.alpha = useFade ? k : 1f;
            slideOffset = useSlide ? new Vector2(Mathf.Lerp(-slideDistance, 0f, k), 0f) : Vector2.zero;
            ApplyTransform();
            yield return null;
        }
        canvasGroup.alpha = 1f;
        slideOffset = Vector2.zero;
        ApplyTransform();
    }

    private IEnumerator AnimateOut()
    {
        float startAlpha = canvasGroup.alpha;
        float t = 0f;
        while (animDuration > 0f && t < animDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / animDuration));
            canvasGroup.alpha = useFade ? Mathf.Lerp(startAlpha, 0f, k) : 1f;
            slideOffset = useSlide ? new Vector2(Mathf.Lerp(0f, -slideDistance, k), 0f) : Vector2.zero;
            ApplyTransform();
            yield return null;
        }
        HideInstant();
    }

    private void HideInstant()
    {
        if (animRoutine != null) { StopCoroutine(animRoutine); animRoutine = null; }
        ClearChoices();
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        slideOffset = Vector2.zero;
        root.SetActive(false);
    }

    private void StartAnim(IEnumerator routine)
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(routine);
    }

    private void LateUpdate()
    {
        ApplyTransform();
    }

    // Puts the view where it should be this frame: bubble follows the character,
    // box sits at its resting spot. The current animation slide offset is added on top of both.
    private void ApplyTransform()
    {
        if (!initialized) return;

        if (followTarget && target != null && Camera.main != null)
        {
            // (Same approach UIManager uses for floating damage text. Needs a Screen Space - Overlay canvas.)
            Vector3 screen = Camera.main.WorldToScreenPoint(target.position + worldOffset);
            root.transform.position = screen + (Vector3)slideOffset;
        }
        else
        {
            rect.anchoredPosition = shownPos + slideOffset;
        }
    }
}
