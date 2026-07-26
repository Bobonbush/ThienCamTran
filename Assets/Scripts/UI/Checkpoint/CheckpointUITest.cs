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
        if (InputManager.Instance.PreviousPress)
        {
            checkpointUI.Open(playerIsOnLeftSide: true);
        }

        // Pretend the player is standing on the right side.
        if (InputManager.Instance.NextPress)
        {
            checkpointUI.Open(playerIsOnLeftSide: false);
        }

        if (InputManager.Instance.Controls.Player.DiscardForceStatement.WasPressedThisFrame())
        {
            checkpointUI.Close();
        }
    }
}
