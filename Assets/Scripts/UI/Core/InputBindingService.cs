using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    public enum MenuBindingId
    {
        Up, Down, Left, Right, Jump, Dash, Attack, Cast, Heal, Inventory
    }

    /// <summary>Which physical device family a rebindable slot targets.</summary>
    public enum BindingDevice
    {
        Keyboard, Gamepad
    }

    /// <summary>
    /// Persists binding overrides on the InputActionAsset used by PlayerInput, and mirrors them onto
    /// every other copy of that asset in play - live PlayerInput clones and any asset registered via
    /// <see cref="RegisterMirror"/> (notably the one InputManager builds from the generated wrapper).
    /// </summary>
    [DefaultExecutionOrder(-95)]
    public sealed class InputBindingService : MonoBehaviour
    {
        /// <summary>One rebindable row: which asset action and composite part it maps to.</summary>
        public sealed class BindingDefinition
        {
            public readonly MenuBindingId id;
            public readonly string displayName;
            public readonly string action;
            public readonly string part;
            public readonly bool gamepadRebindable;

            public BindingDefinition(
                MenuBindingId id,
                string displayName,
                string action,
                string part,
                bool gamepadRebindable)
            {
                this.id = id;
                this.displayName = displayName;
                this.action = action;
                this.part = part;
                this.gamepadRebindable = gamepadRebindable;
            }
        }

        // Order is index-aligned with the control rows built by MainMenuOptionsPanel.
        // Move/Purify only expose per-direction bindings on the keyboard; on a gamepad they are whole
        // stick/dpad bindings, so the four directional rows have nothing to rebind there.
        private static readonly BindingDefinition[] definitions =
        {
            new BindingDefinition(MenuBindingId.Up, "Up", "Purify", "Up", false),
            new BindingDefinition(MenuBindingId.Down, "Down", "Purify", "Down", false),
            new BindingDefinition(MenuBindingId.Left, "Left", "Move", "left", false),
            new BindingDefinition(MenuBindingId.Right, "Right", "Move", "right", false),
            new BindingDefinition(MenuBindingId.Jump, "Jump", "Jump", null, true),
            new BindingDefinition(MenuBindingId.Dash, "Dash", "Dash", null, true),
            new BindingDefinition(MenuBindingId.Attack, "Attack", "Attack", null, true),
            new BindingDefinition(MenuBindingId.Cast, "Cast", "Skill 1", null, true),
            new BindingDefinition(MenuBindingId.Heal, "Heal", "Heal", null, true),
            new BindingDefinition(MenuBindingId.Inventory, "Inventory", "Inventory", null, true)
        };

        public static IReadOnlyList<BindingDefinition> Definitions => definitions;

        public const string StickBindingLabel = "L-Stick / D-Pad";

        private const string OverridesKey = "set_key_overrides";
        private const string LegacyInventoryKey = "set_key_inventory";

        public static InputBindingService Instance { get; private set; }

        public InputActionAsset actions;
        private readonly List<InputActionAsset> mirrors = new List<InputActionAsset>();
        private InputActionRebindingExtensions.RebindingOperation operation;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureExists(null);
        }

        public static InputBindingService EnsureExists(InputActionAsset inputActions)
        {
            if (Instance == null)
            {
                GameObject serviceObject = new GameObject(nameof(InputBindingService));
                Instance = serviceObject.AddComponent<InputBindingService>();
            }

            Instance.Configure(inputActions);
            return Instance;
        }

        public static BindingDefinition GetDefinition(MenuBindingId id)
        {
            foreach (BindingDefinition definition in definitions)
                if (definition.id == id)
                    return definition;
            return null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Inventory used to be rebound on a throwaway action that never reached gameplay; the
            // key it saved to was never in effect, so drop it rather than migrating it.
            if (PlayerPrefs.HasKey(LegacyInventoryKey))
            {
                PlayerPrefs.DeleteKey(LegacyInventoryKey);
                PlayerPrefs.Save();
            }
        }

        private void OnDestroy()
        {
            operation?.Dispose();
            operation = null;
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }

        public void Configure(InputActionAsset inputActions)
        {
            if (inputActions == null) return;
            actions = inputActions;
            LoadSavedOverrides();
        }

        /// <summary>
        /// Registers another copy of the action asset that should receive the same overrides.
        /// Used by InputManager, whose generated wrapper builds its own asset from baked-in defaults.
        /// </summary>
        public void RegisterMirror(InputActionAsset mirror)
        {
            if (mirror == null || mirror == actions) return;
            if (!mirrors.Contains(mirror)) mirrors.Add(mirror);
            ApplyOverridesToMirrors();
        }

        public void UnregisterMirror(InputActionAsset mirror)
        {
            if (mirror == null) return;
            mirrors.Remove(mirror);
        }

        public string GetDisplayString(MenuBindingId id, BindingDevice device)
        {
            if (device == BindingDevice.Gamepad)
            {
                BindingDefinition definition = GetDefinition(id);
                if (definition != null && !definition.gamepadRebindable) return StickBindingLabel;
            }

            InputAction action;
            int index;
            if (!TryResolve(id, device, out action, out index)) return "UNBOUND";
            return action.GetBindingDisplayString(
                index,
                InputBinding.DisplayStringOptions.DontIncludeInteractions);
        }

        public void StartInteractiveRebind(MenuBindingId id, BindingDevice device, Action<bool, string> completed)
        {
            CancelRebind();

            BindingDefinition definition = GetDefinition(id);
            if (device == BindingDevice.Gamepad && (definition == null || !definition.gamepadRebindable))
            {
                completed?.Invoke(false, "NOT REBINDABLE");
                return;
            }

            InputAction action;
            int bindingIndex;
            if (!TryResolve(id, device, out action, out bindingIndex))
            {
                completed?.Invoke(false, "BINDING NOT FOUND");
                return;
            }

            Rebind(action, bindingIndex, device, id, completed);
        }

        /// <summary>
        /// Rebinds an arbitrary action/binding pair. Used by rows that address a binding directly by
        /// <see cref="InputActionReference"/> rather than through the <see cref="MenuBindingId"/> table,
        /// so both paths share one rebind implementation, one conflict rule and one save location.
        /// </summary>
        public void StartInteractiveRebind(
            InputAction action,
            int bindingIndex,
            BindingDevice device,
            Action<bool, string> completed)
        {
            CancelRebind();

            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                completed?.Invoke(false, "BINDING NOT FOUND");
                return;
            }

            // The caller may hold a different instance of the same asset. Rebind our own instance
            // instead, otherwise the override would land on an object SaveOverrides never reads.
            InputActionAsset sourceAsset = action.actionMap != null ? action.actionMap.asset : null;
            if (actions != null && sourceAsset != null && sourceAsset != actions)
            {
                InputAction owned = actions.FindAction(action.id);
                if (owned == null || bindingIndex >= owned.bindings.Count)
                {
                    completed?.Invoke(false, "BINDING NOT FOUND");
                    return;
                }
                action = owned;
            }

            MenuBindingId? conflictScope = null;
            MenuBindingId matched;
            if (TryGetBindingId(action, bindingIndex, device, out matched)) conflictScope = matched;

            Rebind(action, bindingIndex, device, conflictScope, completed);
        }

        /// <summary>Maps a raw action/binding pair back to its table row, if it has one.</summary>
        public bool TryGetBindingId(InputAction action, int bindingIndex, BindingDevice device, out MenuBindingId id)
        {
            id = default;
            if (action == null) return false;

            foreach (BindingDefinition definition in definitions)
            {
                InputAction candidate;
                int candidateIndex;
                if (!TryResolve(definition.id, device, out candidate, out candidateIndex)) continue;
                if (candidate.id == action.id && candidateIndex == bindingIndex)
                {
                    id = definition.id;
                    return true;
                }
            }
            return false;
        }

        private void Rebind(
            InputAction action,
            int bindingIndex,
            BindingDevice device,
            MenuBindingId? conflictScope,
            Action<bool, string> completed)
        {
            string previousOverride = action.bindings[bindingIndex].overridePath;
            bool wasEnabled = action.enabled;
            action.Disable();

            InputActionRebindingExtensions.RebindingOperation pending =
                action.PerformInteractiveRebinding(bindingIndex);

            if (device == BindingDevice.Gamepad)
            {
                pending = pending
                    .WithControlsHavingToMatchPath("<Gamepad>")
                    .WithExpectedControlType("Button");
            }
            else
            {
                pending = pending
                    .WithControlsExcluding("<Mouse>/position")
                    .WithControlsExcluding("<Mouse>/delta")
                    .WithControlsExcluding("<Mouse>/scroll")
                    .WithControlsHavingToMatchPath("<Keyboard>");
            }

            // Always cancel through the keyboard so a dead or unplugged controller cannot trap the
            // player in a capture prompt.
            operation = pending
                .WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(op =>
                {
                    FinishOperation(op, action, wasEnabled);
                    completed?.Invoke(false, "REBIND CANCELLED");
                })
                .OnComplete(op =>
                {
                    string newPath = action.bindings[bindingIndex].effectivePath;
                    if (conflictScope.HasValue && HasConflict(conflictScope.Value, device, newPath))
                    {
                        if (string.IsNullOrEmpty(previousOverride))
                            action.RemoveBindingOverride(bindingIndex);
                        else
                            action.ApplyBindingOverride(bindingIndex, previousOverride);
                        FinishOperation(op, action, wasEnabled);
                        completed?.Invoke(false, "KEY ALREADY IN USE");
                        return;
                    }

                    FinishOperation(op, action, wasEnabled);
                    ApplyOverridesToMirrors();
                    completed?.Invoke(true, action.GetBindingDisplayString(
                        bindingIndex,
                        InputBinding.DisplayStringOptions.DontIncludeInteractions));
                })
                .Start();
        }

        public void CancelRebind()
        {
            if (operation == null) return;
            operation.Cancel();
        }

        public void SaveOverrides()
        {
            if (actions != null)
                PlayerPrefs.SetString(OverridesKey, actions.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
            ApplyOverridesToMirrors();
        }

        public void RevertUnsaved()
        {
            LoadSavedOverrides();
        }

        public void ResetToDefaults()
        {
            CancelRebind();
            if (actions != null)
                foreach (InputActionMap map in actions.actionMaps)
                    map.RemoveAllBindingOverrides();

            PlayerPrefs.DeleteKey(OverridesKey);
            PlayerPrefs.Save();
            ApplyOverridesToMirrors();
        }

        public bool WasPressedThisFrame(MenuBindingId id, BindingDevice device)
        {
            InputAction action;
            int bindingIndex;
            if (!TryResolve(id, device, out action, out bindingIndex)) return false;
            string path = action.bindings[bindingIndex].effectivePath;
            ButtonControl control = InputSystem.FindControl(path) as ButtonControl;
            return control != null && control.wasPressedThisFrame;
        }

        private void LoadSavedOverrides()
        {
            CancelRebind();
            if (actions != null)
            {
                foreach (InputActionMap map in actions.actionMaps)
                    map.RemoveAllBindingOverrides();
                string json = PlayerPrefs.GetString(OverridesKey, string.Empty);
                if (!string.IsNullOrEmpty(json))
                {
                    try { actions.LoadBindingOverridesFromJson(json); }
                    catch (Exception exception)
                    {
                        Debug.LogWarning($"InputBindingService: ignored invalid saved overrides. {exception.Message}");
                        PlayerPrefs.DeleteKey(OverridesKey);
                    }
                }
            }

            ApplyOverridesToMirrors();
        }

        private bool TryResolve(MenuBindingId id, BindingDevice device, out InputAction action, out int bindingIndex)
        {
            action = null;
            bindingIndex = -1;

            BindingDefinition definition = GetDefinition(id);
            if (actions == null || definition == null) return false;

            action = actions.FindAction(definition.action, false);
            if (action == null) return false;

            string devicePrefix = device == BindingDevice.Gamepad ? "<Gamepad>" : "<Keyboard>";
            bool wantsPart = definition.part != null;

            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];

                // Composite headers ("Dpad") carry no path of their own.
                if (binding.isComposite) continue;
                if (binding.isPartOfComposite != wantsPart) continue;
                if (wantsPart &&
                    !string.Equals(binding.name, definition.part, StringComparison.OrdinalIgnoreCase))
                    continue;

                // effectivePath, not path: an already-overridden binding must still resolve to its
                // own row rather than falling through to a later binding on the same action.
                string path = binding.effectivePath;
                if (path == null ||
                    !path.StartsWith(devicePrefix, StringComparison.OrdinalIgnoreCase)) continue;

                bindingIndex = i;
                return true;
            }

            action = null;
            return false;
        }

        private bool HasConflict(MenuBindingId changedId, BindingDevice device, string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            foreach (BindingDefinition definition in definitions)
            {
                if (definition.id == changedId) continue;
                // A keyboard key never conflicts with a gamepad button.
                InputAction otherAction;
                int otherIndex;
                if (!TryResolve(definition.id, device, out otherAction, out otherIndex)) continue;
                if (string.Equals(
                    otherAction.bindings[otherIndex].effectivePath,
                    path,
                    StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private void FinishOperation(
            InputActionRebindingExtensions.RebindingOperation completedOperation,
            InputAction action,
            bool reEnable)
        {
            completedOperation.Dispose();
            operation = null;
            if (reEnable) action.Enable();
        }

        private void ApplyOverridesToMirrors()
        {
            if (actions == null) return;
            string json = actions.SaveBindingOverridesAsJson();

            for (int i = mirrors.Count - 1; i >= 0; i--)
            {
                InputActionAsset mirror = mirrors[i];
                if (mirror == null)
                {
                    mirrors.RemoveAt(i);
                    continue;
                }
                ApplyJson(mirror, json, mirror.name);
            }

            // PlayerInput clones the asset when more than one player exists, so live clones still
            // need the sweep even though the registered mirrors cover InputManager.
            foreach (PlayerInput playerInput in FindObjectsByType<PlayerInput>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None))
            {
                if (playerInput.actions == null || playerInput.actions == actions) continue;
                if (mirrors.Contains(playerInput.actions)) continue;
                ApplyJson(playerInput.actions, json, playerInput.name);
            }
        }

        private static void ApplyJson(InputActionAsset target, string json, string label)
        {
            try { target.LoadBindingOverridesFromJson(json); }
            catch (Exception exception)
            {
                Debug.LogWarning($"InputBindingService: could not update '{label}'. {exception.Message}");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (actions == null)
            {
                PlayerInput playerInput = FindFirstObjectByType<PlayerInput>(FindObjectsInactive.Include);
                if (playerInput != null && playerInput.actions != null)
                {
                    actions = playerInput.actions;
                    LoadSavedOverrides();
                    return;
                }
            }
            ApplyOverridesToMirrors();
        }
    }
}
