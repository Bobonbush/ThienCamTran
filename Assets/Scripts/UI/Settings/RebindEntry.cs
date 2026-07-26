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
    /// index. The rebind itself is run by <see cref="InputBindingService"/> so this row gets the same
    /// device filtering, duplicate-key rejection and override propagation as the main-menu panel.
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

        private InputAction Action => actionReference != null ? actionReference.action : null;

        private void Awake()
        {
            if (rebindButton != null) rebindButton.onClick.AddListener(StartRebind);
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
            InputAction action = Action;
            InputBindingService service = InputBindingService.Instance;
            if (action == null || service == null) return;

            if (waitingOverlay != null) waitingOverlay.SetActive(true);
            if (keyLabel != null) keyLabel.text = "...";

            service.StartInteractiveRebind(
                action,
                bindingIndex,
                BindingDevice.Keyboard,
                (success, message) =>
                {
                    if (waitingOverlay != null) waitingOverlay.SetActive(false);
                    RefreshLabel();
                    if (success) manager?.SaveOverrides();
                });
        }
    }
}
