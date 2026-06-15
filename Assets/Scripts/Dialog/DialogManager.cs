using System;
using UnityEngine;

// Picks the display style for a conversation
public enum DialogStyle { Box, Bubble }

// The brain of the dialog: handles the branching logic (pick A go this way, pick B go that way).
// All display is delegated to DialogView, so every style shares this same logic.
public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance { get; private set; }

    [Header("The two display styles")]
    public DialogView boxView;            // rectangular box at the bottom
    public DialogView bubbleView;         // speech bubble above a character

    // Fired when the conversation ends -> e.g. the Boss listens to this to start the fight.
    public event Action DialogEnded;

    private DialogView active;            // the view used by the current conversation

    private void Awake()
    {
        Instance = this;
        if (boxView != null) boxView.Close();
        if (bubbleView != null) bubbleView.Close();
    }

    // style = box or bubble.
    // target = the character the bubble follows (only needed for the Bubble style).
    public void StartDialog(DialogNode startNode, DialogStyle style, Transform target = null)
    {
        active = (style == DialogStyle.Bubble) ? bubbleView : boxView;
        if (style == DialogStyle.Bubble) active.SetTarget(target);

        active.Open();
        ShowNode(startNode);
    }

    private void ShowNode(DialogNode node)
    {
        active.SetText(node.speakerName, node.text);
        active.ClearChoices();

        if (node.IsEnd)
        {
            active.SpawnChoice("Close", EndDialog);
            return;
        }

        // ===== BRANCHING (the "if-else") =====
        // Each choice jumps to its own nextNode. Pick A (choices[0]) -> node C,
        // pick B (choices[1]) -> node D.
        foreach (DialogChoice choice in node.choices)
        {
            DialogNode nextNode = choice.nextNode;  // copy to a local so the lambda captures the right value
            active.SpawnChoice(choice.choiceText, () => OnChoiceSelected(nextNode));
        }
    }

    private void OnChoiceSelected(DialogNode nextNode)
    {
        if (nextNode == null) { EndDialog(); return; }  // points nowhere => end
        ShowNode(nextNode);                              // jump to the matching branch
    }

    public void EndDialog()
    {
        if (active != null) active.Close();
        active = null;
        DialogEnded?.Invoke();
    }
}
