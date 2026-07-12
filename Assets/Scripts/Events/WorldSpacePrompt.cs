using System.Collections;
using System.Xml;
using TMPro;
using UnityEngine;

public class WorldSpacePrompt : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshPro tmpText;

    [Header("Settings")]
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private float slideDistance = 0.5f;

    

    private Coroutine fadeCoroutine;
    private Vector3 baseLocalPosition;
    private Coroutine activeAnimation;

    bool isStillOneMoreRound = false;
    public enum Type
    {
        None,
        NeedTrigger
    };

    [SerializeField]
    private Type type = Type.None;


    [SerializeField]
    private float TriggerDuration = 1.0f;

    void Start()
    {
        tmpText = GetComponentInChildren<TextMeshPro>();
        if (tmpText == null)
        {
            Debug.LogError($"Please add a TextMeshPro component as a child of {gameObject.name}");
            return;
        }
        // Start hidden and lowered
        baseLocalPosition = tmpText.transform.localPosition;
        SetTutorialState(0f);

        if(type == Type.NeedTrigger)
        {
            GetComponent<BoxCollider2D>().enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Make sure your Player GameObject has the tag "Player"
        if (other.CompareTag("Player") && type == Type.None)
        {
            StartAnimation(1f); // Fade in and slide up
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && type == Type.None)
        {
            StartAnimation(0f); // Fade out and slide down
        }
    }


    public void OutSideActivate()
    {
        if (type != Type.NeedTrigger) return;

        if (activeAnimation != null) StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(ActiveAndOff());
    }

    private IEnumerator ActiveAndOff()
    {
        yield return AnimateTutorial(1f);
        float t = 0f;
        isStillOneMoreRound = true;
        while (t < TriggerDuration)
        {
            yield return null;
            t += Time.deltaTime;
        }
        isStillOneMoreRound = false;

        yield return AnimateTutorial(0f);
        activeAnimation = null;
    }

    
    

    private void StartAnimation(float targetState)
    {
        if (activeAnimation != null) StopCoroutine(activeAnimation);
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

        if(type == Type.None)
        {
           activeAnimation = null;
        }
    }

    private void SetTutorialState(float alpha)
    {
        Color c = tmpText.color;
        c.a = alpha;
        tmpText.color = c;
        tmpText.transform.localPosition = baseLocalPosition + new Vector3(0, alpha * slideDistance, 0);
    }


    public bool FinishAnimation()
    {
        return activeAnimation == null;
    }

}
