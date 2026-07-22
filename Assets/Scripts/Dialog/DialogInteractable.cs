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

    public bool CanInteract
    {
        get
        {
            ResolvePlayer();
            return isActiveAndEnabled && !OutSideTrigger && player != null &&
                   Vector2.Distance(transform.position, player.position) <= interactRange &&
                   dialogNodes != null && dialogNodes.Count > 0 &&
                   DialogManager.Instance != null && !DialogManager.Instance.IsActive;
        }
    }

    private void Awake()
    {
        if (promptObject != null)
            promptObject.SetActive(false);

        ResolvePlayer();

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

    private void Update()
    {
        if (OutSideTrigger)
        {
            return;
        }
        bool canInteract = CanInteract;

        if (promptObject != null && promptObject.activeSelf != canInteract)
            promptObject.SetActive(canInteract);
    }

    public void TriggerInteract()
    {
        if (!OutSideTrigger)
        {
            return;
        }
        StartInteraction();
    }

    public void Interact(PlayerController activePlayer)
    {
        if (CanInteract) StartInteraction();
    }

    public new IInteractable.Type GetType() => IInteractable.Type.Dialog;

    private void ResolvePlayer()
    {
        if (player != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
    }

    private void StartInteraction()
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
