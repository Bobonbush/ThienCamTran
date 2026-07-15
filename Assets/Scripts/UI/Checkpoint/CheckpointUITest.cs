using UnityEngine;
using UnityEngine.InputSystem;

public class CheckpointUITest : MonoBehaviour
{
    [SerializeField] private CheckpointWorldUI checkpointUI;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || checkpointUI == null)
            return;

        // Pretend the player is standing on the left side.
        if (keyboard.leftBracketKey.wasPressedThisFrame)
        {
            checkpointUI.Open(playerIsOnLeftSide: true);
        }

        // Pretend the player is standing on the right side.
        if (keyboard.rightBracketKey.wasPressedThisFrame)
        {
            checkpointUI.Open(playerIsOnLeftSide: false);
        }

        if (keyboard.backslashKey.wasPressedThisFrame)
        {
            checkpointUI.Close();
        }
    }
}
