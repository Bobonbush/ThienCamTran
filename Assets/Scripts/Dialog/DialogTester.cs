using UnityEngine;
using UnityEngine.InputSystem;

// TEMPORARY script to test the dialog with a key. Attach to the Player, press F to open.
// Safe to delete once you're done testing.
public class DialogTester : MonoBehaviour
{
    public DialogNode startNode;                 // First node
    public DialogStyle style = DialogStyle.Box;  // Box or Bubble
    public Transform bubbleTarget;               // Who the bubble follows (empty = this object)

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (DialogManager.Instance == null || startNode == null) return;

            Transform target = bubbleTarget != null ? bubbleTarget : transform;
            DialogManager.Instance.StartDialog(startNode, style, target);
        }
    }
}
