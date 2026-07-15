using TMPro;
using UnityEngine;
using System.Collections;
public class TutorialPrompt : MonoBehaviour
{
    [Header("Tutorial Settings")]
    [Tooltip("The button name, e.g., E, Space, Left Click")]
    [SerializeField] private string inputKeyName = "E";
    [SerializeField] private string actionDescription = "Open Chest";
    [SerializeField] private string additionalDetail = "";

    [Header("Animation Settings")]
    [SerializeField] private float fadeSpeed = 4f;
    [SerializeField] private float slideDistance = 0.5f;

    private TextMeshPro tmpText;
    private Vector3 baseLocalPosition;
    private Coroutine activeAnimation;


    [SerializeField] private bool isHoldAction = true;
    [SerializeField] private bool TapSequence = false;

    [Header("Sequence Settings")]
    [SerializeField] private string firstKey = "Down";      
    [SerializeField] private string secondKey = "Z";      
    [SerializeField] bool usingSequence = false;

    void Awake()
    {
        
        // 1. Automatically find or create the Text Component to save time
        tmpText = GetComponentInChildren<TextMeshPro>();
        if (tmpText == null)
        {
            Debug.LogError($"Please add a TextMeshPro component as a child of {gameObject.name}");
            return;
        }

        // 2. Convenience: Automatically format the text with color highlighting
        // Uses standard TMPro rich text tags for colors and bolding

        string actionPrefix = isHoldAction ? "Hold" : "Press";

        string sequence = TapSequence ? " Rapidly" : "";
        
        tmpText.text =   $"{actionPrefix} <color=#FFD700><b>[{inputKeyName}]</b></color>{sequence}{additionalDetail} to {actionDescription}";

        if(usingSequence)
        {
            tmpText.text = tmpText.text = $"Hold <color=#FFD700><b>[{firstKey}]</b></color> + Press <color=#FFD700><b>[{secondKey}]</b></color>{additionalDetail} to {actionDescription}";
        }

        // Record starting position for the slide animation
        baseLocalPosition = tmpText.transform.localPosition;

        // Start hidden and lowered
        SetTutorialState(0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StartAnimation(1f); // Fade in and slide up
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StartAnimation(0f); // Fade out and slide down
        }
    }

    private void StartAnimation(float targetState)
    {
        if (tmpText == null)
            return;

        if (activeAnimation != null)
        {
            StopCoroutine(activeAnimation);
            activeAnimation = null;
        }

        // Disabling a trigger invokes OnTriggerExit2D after the GameObject may
        // already be inactive. Coroutines cannot start in that state.
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            SetTutorialState(targetState);
            return;
        }

        activeAnimation = StartCoroutine(AnimateTutorial(targetState));
    }

    private IEnumerator AnimateTutorial(float targetAlpha)
    {
        Color color = tmpText.color;
        Vector3 targetPosition = baseLocalPosition + new Vector3(0, targetAlpha * slideDistance, 0);

        // Smoothly interpolate both Alpha and Position at the same time
        while (!Mathf.Approximately(color.a, targetAlpha))
        {
            // Fade
            color.a = Mathf.MoveTowards(color.a, targetAlpha, fadeSpeed * Time.deltaTime);
            tmpText.color = color;

            // Slide
            tmpText.transform.localPosition = Vector3.Lerp(tmpText.transform.localPosition, targetPosition, fadeSpeed * Time.deltaTime);

            yield return null;
        }

        SetTutorialState(targetAlpha);
        activeAnimation = null;
    }

    private void OnDisable()
    {
        if (activeAnimation != null)
        {
            StopCoroutine(activeAnimation);
            activeAnimation = null;
        }

        if (tmpText != null)
            SetTutorialState(0f);
    }

    private void SetTutorialState(float alpha)
    {
        Color c = tmpText.color;
        c.a = alpha;
        tmpText.color = c;
        tmpText.transform.localPosition = baseLocalPosition + new Vector3(0, alpha * slideDistance, 0);
    }
}
