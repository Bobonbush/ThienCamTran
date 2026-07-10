using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.UI
{
    /// <summary>
    /// Keyboard category (doc image rId37, "looks exactly like this"). Owns the shared
    /// <see cref="InputActionAsset"/> and handles persistence + reset-to-defaults for the whole
    /// rebind grid. Individual rows are <see cref="RebindEntry"/> components that ask this manager
    /// to save after a successful rebind.
    ///
    /// IMPORTANT: assign the SAME asset instance the player uses in gameplay (the one referenced by
    /// the PlayerInput component) so overrides actually take effect. Overrides are stored as Input
    /// System JSON in PlayerPrefs and re-applied on startup.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class KeyboardRebindManager : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private RebindEntry[] entries;

        private const string PrefKey = "set_key_overrides";

        public InputActionAsset Actions => inputActions;

        private void Awake()
        {
            LoadOverrides();
        }

        private void OnEnable()
        {
            RefreshAllLabels();
        }

        public void LoadOverrides()
        {
            if (inputActions == null) return;
            string json = PlayerPrefs.GetString(PrefKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
                inputActions.LoadBindingOverridesFromJson(json);
        }

        public void SaveOverrides()
        {
            if (inputActions == null) return;
            PlayerPrefs.SetString(PrefKey, inputActions.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }

        /// <summary>Footer "RESET DEFAULTS": clear every override and persist the cleared state.</summary>
        public void ResetToDefaults()
        {
            if (inputActions == null) return;
            foreach (var map in inputActions.actionMaps)
                map.RemoveAllBindingOverrides();

            PlayerPrefs.DeleteKey(PrefKey);
            PlayerPrefs.Save();
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
