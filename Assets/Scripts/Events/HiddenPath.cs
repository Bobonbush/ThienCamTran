using UnityEngine;
using System.Collections;
public class HiddenPath : MonoBehaviour
{
    [Header("Fade Settings")]
    [Tooltip("How long the fade transition takes in seconds.")]
    [SerializeField] private float fadeDuration = 0.5f;

    private int triggerCount = 0;
    private Coroutine fadeCoroutine;
    private SpriteRenderer[] childSprites;


    private void Start()
    {
        // Cache all child SpriteRenderers
        childSprites = GetComponentsInChildren<SpriteRenderer>();

        // Force alpha to 255 (fully opaque) on startup
        SetAlphaImmediate(1f);

    }

    

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggerCount == 0)
            {
                // Player entered -> Fade OUT (Alpha 255 to 0)
                StartFade(0f);
            }
            triggerCount++;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            triggerCount--;

            if (triggerCount <= 0)
            {
                triggerCount = 0; // Safety reset
                // Player left -> Fade IN (Alpha 0 to 255)
                StartFade(1f);
            }
        }
    }

    public void StartFade(float targetAlpha)
    {
        // Stop any running fade so they don't fight each other
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float elapsedTime = 0f;

        // Grab the starting alpha from the first child to use as a baseline
        float startAlpha = childSprites.Length > 0 ? childSprites[0].color.a : 1f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            // Smoothly calculate the current alpha point between start and target
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);

            UpdateChildrenAlpha(newAlpha);
            yield return null; // Wait for the next frame
        }

        // Ensure we strictly hit the exact final target value
        UpdateChildrenAlpha(targetAlpha);
    }

    private void UpdateChildrenAlpha(float alpha)
    {
        foreach (var sprite in childSprites)
        {
            if (sprite != null)
            {
                Color color = sprite.color;
                color.a = alpha;
                sprite.color = color;
            }
        }
    }

    private void SetAlphaImmediate(float alpha)
    {
        UpdateChildrenAlpha(alpha);
    }
}
