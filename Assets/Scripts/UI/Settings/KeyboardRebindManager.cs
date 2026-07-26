using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.UI
{
    /// <summary>
    /// Keyboard category (doc image rId37, "looks exactly like this") for the shared Options screen.
    ///
    /// Persistence, conflict detection and override propagation all live in
    /// <see cref="InputBindingService"/>; this component only owns the grid's serialized asset
    /// reference and forwards to that service. It deliberately does NOT touch PlayerPrefs itself -
    /// it used to write the same "set_key_overrides" key as the service, so whichever saved last won.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class KeyboardRebindManager : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private RebindEntry[] entries;

        public InputActionAsset Actions => inputActions;

        private void Awake()
        {
            // Hands the grid's asset to the service if nothing has claimed one yet, and pulls the
            // saved overrides back onto it.
            InputBindingService.EnsureExists(inputActions);
        }

        private void OnEnable()
        {
            RefreshAllLabels();
        }

        public void SaveOverrides()
        {
            InputBindingService.Instance?.SaveOverrides();
        }

        /// <summary>Footer "RESET DEFAULTS": clear every override and persist the cleared state.</summary>
        public void ResetToDefaults()
        {
            InputBindingService.Instance?.ResetToDefaults();
            RefreshAllLabels();
        }

        public void RefreshAllLabels()
        {
            if (entries == null) return;
            foreach (var entry in entries)
                if (entry != null) entry.RefreshLabel();
        }
    }
}
