using TMPro;
using UnityEngine;
using System.Collections;

public class WorldSpacePrompt : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshPro promptText;

    [Header("Settings")]
    [SerializeField] private float fadeSpeed = 5f;

    private Coroutine fadeCoroutine;

    void Start()
    {
        // Start completely invisible
        SetTextAlpha(0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Make sure your Player GameObject has the tag "Player"
        if (other.CompareTag("Player"))
        {
            TriggerFade(1f); // Fade In
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            TriggerFade(0f); // Fade Out
        }
    }

    private void TriggerFade(float targetAlpha)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeText(targetAlpha));
    }

    private IEnumerator FadeText(float targetAlpha)
    {
        Color color = promptText.color;
        while (!Mathf.Approximately(color.a, targetAlpha))
        {
            color.a = Mathf.MoveTowards(color.a, targetAlpha, fadeSpeed * Time.deltaTime);
            promptText.color = color;
            yield return null;
        }
    }

    private void SetTextAlpha(float alpha)
    {
        Color color = promptText.color;
        color.a = alpha;
        promptText.color = color;
    }
}
