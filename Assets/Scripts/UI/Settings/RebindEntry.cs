using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// One row of the keyboard rebind grid: an action label ("Jump", "Attack"...) plus a clickable
    /// key box that shows the current key and starts an interactive rebind when pressed.
    ///
    /// Targets a single binding of a single action via <see cref="InputActionReference"/> + binding
    /// index. The Keyboard&amp;Mouse-only scheme keeps the rebind on the right device.
    /// </summary>
    public class RebindEntry : MonoBehaviour
    {
        [SerializeField] private KeyboardRebindManager manager;
        [SerializeField] private InputActionReference actionReference;
        [Tooltip("Which binding index of the action to rebind (0 for simple buttons; pick the " +
                 "Keyboard part for composites like Move).")]
        [SerializeField] private int bindingIndex = 0;

        [Header("UI")]
        [SerializeField] private TMP_Text keyLabel;
        [SerializeField] private Button rebindButton;
        [SerializeField] private GameObject waitingOverlay; // optional "Press a key..." indicator

        private InputActionRebindingExtensions.RebindingOperation rebindOp;

        private InputAction Action => actionReference != null ? actionReference.action : null;

        private void Awake()
        {
            if (rebindButton != null) rebindButton.onClick.AddListener(StartRebind);
        }

        private void OnDestroy()
        {
            rebindOp?.Dispose();
        }

        public void RefreshLabel()
        {
            if (keyLabel == null || Action == null) return;
            keyLabel.text = Action.GetBindingDisplayString(
                bindingIndex,
                InputBinding.DisplayStringOptions.DontIncludeInteractions);
        }

        public void StartRebind()
        {
            var action = Action;
            if (action == null) return;

            if (waitingOverlay != null) waitingOverlay.SetActive(true);
            if (keyLabel != null) keyLabel.text = "...";

            // Disable while rebinding (required by the Input System), then re-enable on completion.
            bool wasEnabled = action.enabled;
            action.Disable();

            rebindOp?.Dispose();
            rebindOp = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(op => Finish(op, wasEnabled))
                .OnCancel(op => Finish(op, wasEnabled))
                .Start();
        }

        private void Finish(InputActionRebindingExtensions.RebindingOperation op, bool reEnable)
        {
            op.Dispose();
            rebindOp = null;

            if (reEnable) Action?.Enable();
            if (waitingOverlay != null) waitingOverlay.SetActive(false);

            RefreshLabel();
            if (manager != null) manager.SaveOverrides();
        }
    }
}
