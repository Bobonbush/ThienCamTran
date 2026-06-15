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

    private Transform target;
    private readonly List<Button> spawnedButtons = new List<Button>();

    public void SetTarget(Transform t) => target = t;

    public void Open() => root.SetActive(true);

    public void Close()
    {
        ClearChoices();
        root.SetActive(false);
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

    private void LateUpdate()
    {
        // Bubble: each frame recompute the screen position from the character's world position.
        // (Same approach UIManager uses for floating damage text. Needs a Screen Space - Overlay canvas.)
        if (followTarget && target != null && Camera.main != null)
        {
            root.transform.position = Camera.main.WorldToScreenPoint(target.position + worldOffset);
        }
    }
}
