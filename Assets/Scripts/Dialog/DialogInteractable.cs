using UnityEngine;
using UnityEngine.InputSystem;

// Put this on a prefab (NPC, chest, sign...). When the Player is within range it shows an "E"
// prompt above the object; pressing E starts the dialog.
//
// Detection is a DISTANCE check (not a trigger collider), so it keeps working no matter how the
// object's physics are set up — e.g. an enemy that has gravity but lets the player walk through it.
public class DialogInteractable : MonoBehaviour
{
    [Header("Dialog")]
    public DialogNode startNode;                 // First node of the conversation
    public DialogStyle style = DialogStyle.Box;  // Box or bubble
    public Transform bubbleTarget;               // Bubble follows this (empty = this object)

    [Header("Prompt")]
    public GameObject promptObject;              // The "E" indicator above the head (a child object)
    public float interactRange = 2f;             // how close the player must be to interact

    [Header("Interaction")]
    public bool triggerOnce = false;             // Allow talking only once?
    public bool playerPassesThrough = true;      // Let the player walk through this object (keeps gravity)

    private Transform player;
    private bool used;

    private void Awake()
    {
        if (promptObject != null) promptObject.SetActive(false);

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        // Make the player pass through this object's colliders while it still falls/rests on the
        // ground normally. Triggers are unaffected, so the distance check below keeps working.
        if (playerPassesThrough && player != null)
        {
            Collider2D[] mine = GetComponentsInChildren<Collider2D>();
            Collider2D[] theirs = player.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D a in mine)
                foreach (Collider2D b in theirs)
                    Physics2D.IgnoreCollision(a, b, true);
        }
    }

    private void Update()
    {
        bool inRange = player != null &&
                       Vector2.Distance(transform.position, player.position) <= interactRange;

        bool canInteract = inRange
                           && startNode != null
                           && !(triggerOnce && used)
                           && DialogManager.Instance != null
                           && !DialogManager.Instance.IsActive;   // not while another dialog is open

        // Show/hide the "E" prompt to match.
        if (promptObject != null && promptObject.activeSelf != canInteract)
            promptObject.SetActive(canInteract);

        // Press E to start the dialog.
        if (canInteract && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            Interact();
    }

    private void Interact()
    {
        used = true;
        if (promptObject != null) promptObject.SetActive(false);

        Transform target = bubbleTarget != null ? bubbleTarget : transform;
        DialogManager.Instance.StartDialog(startNode, style, target);
    }

    // Draw the interaction range in the Scene view when selected.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
