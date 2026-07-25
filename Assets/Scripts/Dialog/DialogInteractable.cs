using System.Collections.Generic;
using UnityEngine;

// Put this on a prefab (NPC, chest, sign...). When the Player is within range it shows an "E"
// prompt above the object; pressing E starts the dialog.
//
// Detection is a DISTANCE check (not a trigger collider), so it keeps working no matter how the
// object's physics are set up — e.g. an enemy that has gravity but lets the player walk through it.
public class DialogInteractable : MonoBehaviour, IInteractable
{
    [Header("Dialog")]
    public List<DialogNode> dialogNodes = new List<DialogNode>();

    public DialogStyle style = DialogStyle.Box;

    public Transform bubbleTarget;

    [Header("Prompt")]
    public GameObject promptObject;
    public float interactRange = 2f;

    [Header("Interaction")]
    public bool triggerOnce = false;
    public bool playerPassesThrough = true;
    public bool OutSideTrigger = false;

    private Transform player;

    // Which dialogue should be played next
    private int currentDialogIndex = 0;


    private void Awake()
    {
        if (promptObject != null)
            promptObject.SetActive(false);


        if (playerPassesThrough && player != null)
        {
            Collider2D[] mine = GetComponentsInChildren<Collider2D>();
            Collider2D[] theirs = player.GetComponentsInChildren<Collider2D>();

            foreach (Collider2D a in mine)
            {
                foreach (Collider2D b in theirs)
                {
                    Physics2D.IgnoreCollision(a, b, true);
                }
            }
        }
    }

    public bool CanInteract
    {
        get {
            return dialogNodes != null &&
            dialogNodes.Count > 0 &&
            DialogManager.Instance != null &&
            !DialogManager.Instance.IsActive; }
    }

    private void Update()
    {
        if (OutSideTrigger)
        {
            return;
        }

        
    }

    public void SetPromptVisible(bool visible)
    {
        bool canShow = visible && CanInteract;
        if (promptObject != null && promptObject.activeSelf != canShow)
            promptObject.SetActive(canShow);
    }

    public void TriggerInteract()
    {
        if (!OutSideTrigger)
        {
            return;
        }
        Interact();
    }

    public IInteractable.Type GetType()
    {
        return IInteractable.Type.Dialog;
    }

    public void Interact(PlayerController controller)
    {
        if (promptObject != null)
            promptObject.SetActive(false);


        // Get the current dialogue
        DialogNode node = dialogNodes[currentDialogIndex];

        Transform target =
            bubbleTarget != null
            ? bubbleTarget
            : transform;

        DialogManager.Instance.StartDialog(node, style, target);

        // Move to the next dialogue
        if (currentDialogIndex < dialogNodes.Count - 1)
        {
            currentDialogIndex++;
        }

        if (triggerOnce)
        {
            this.enabled = false;
        }
    }

    private void Interact()
    {
        if (promptObject != null)
            promptObject.SetActive(false);

        // Get the current dialogue
        DialogNode node = dialogNodes[currentDialogIndex];

        Transform target =
            bubbleTarget != null
            ? bubbleTarget
            : transform;

        DialogManager.Instance.StartDialog(node, style, target);

        // Move to the next dialogue
        if (currentDialogIndex < dialogNodes.Count - 1)
        {
            currentDialogIndex++;
        }

        if (triggerOnce) {
            this.enabled = false;
        }
        // If already at the last dialogue,
        // stay there forever and replay it.
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            interactRange
        );
    }
}
