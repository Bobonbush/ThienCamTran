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

    /// <summary>
    /// Persists keyboard binding overrides on the same InputActionAsset used by PlayerInput.
    /// Overrides are copied to live PlayerInput clones so changes also work from an in-game menu.
    /// </summary>
    [DefaultExecutionOrder(-95)]
    public sealed class InputBindingService : MonoBehaviour
    {
        private sealed class BindingTarget
        {
            public string action;
            public string part;
            public BindingTarget(string actionName, string compositePart = null)
            {
                action = actionName;
                part = compositePart;
            }
        }

        private static readonly Dictionary<MenuBindingId, BindingTarget> Targets =
            new Dictionary<MenuBindingId, BindingTarget>
            {
                { MenuBindingId.Up, new BindingTarget("Purify", "Up") },
                { MenuBindingId.Down, new BindingTarget("Purify", "Down") },
                { MenuBindingId.Left, new BindingTarget("Move", "left") },
                { MenuBindingId.Right, new BindingTarget("Move", "right") },
                { MenuBindingId.Jump, new BindingTarget("Jump") },
                { MenuBindingId.Dash, new BindingTarget("Dash") },
                { MenuBindingId.Attack, new BindingTarget("Attack") },
                { MenuBindingId.Cast, new BindingTarget("Skill 1") },
                { MenuBindingId.Heal, new BindingTarget("Heal") }
            };

        private const string OverridesKey = "set_key_overrides";
        private const string InventoryKey = "set_key_inventory";
        private const string DefaultInventoryPath = "<Keyboard>/i";

        public static InputBindingService Instance { get; private set; }

        public InputActionAsset actions;
        private InputAction inventoryAction;
        private InputActionRebindingExtensions.RebindingOperation operation;
        private string savedInventoryPath = DefaultInventoryPath;

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
            CreateInventoryAction();

            Debug.Log($"Has Key: {PlayerPrefs.HasKey(OverridesKey)}");
            Debug.Log($"Key Name: {OverridesKey}");
            Debug.Log($"Value: '{PlayerPrefs.GetString(OverridesKey)}'");
        }

        private void OnDestroy()
        {
            operation?.Dispose();
            inventoryAction?.Dispose();
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

        public string GetDisplayString(MenuBindingId id)
        {
            InputAction action;
            int index;
            if (!TryResolve(id, out action, out index)) return "UNBOUND";
            return action.GetBindingDisplayString(
                index,
                InputBinding.DisplayStringOptions.DontIncludeInteractions);
        }

        public void StartInteractiveRebind(MenuBindingId id, Action<bool, string> completed)
        {
            CancelRebind();

            InputAction action;
            int bindingIndex;
            if (!TryResolve(id, out action, out bindingIndex))
            {
                completed?.Invoke(false, "BINDING NOT FOUND");
                return;
            }

            string previousOverride = action.bindings[bindingIndex].overridePath;
            bool wasEnabled = action.enabled;
            action.Disable();

            operation = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Mouse>/scroll")
                .WithControlsHavingToMatchPath("<Keyboard>")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(op =>
                {
                    FinishOperation(op, action, wasEnabled);
                    completed?.Invoke(false, "REBIND CANCELLED");
                })
                .OnComplete(op =>
                {
                    string newPath = action.bindings[bindingIndex].effectivePath;
                    if (HasConflict(id, newPath))
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
                    ApplyOverridesToLivePlayers();
                    completed?.Invoke(true, GetDisplayString(id));
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
            savedInventoryPath = inventoryAction != null
                ? inventoryAction.bindings[0].effectivePath
                : DefaultInventoryPath;
            PlayerPrefs.SetString(InventoryKey, savedInventoryPath);
            PlayerPrefs.Save();
            ApplyOverridesToLivePlayers();
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

            if (inventoryAction != null)
                inventoryAction.RemoveAllBindingOverrides();
            savedInventoryPath = DefaultInventoryPath;
            PlayerPrefs.DeleteKey(OverridesKey);
            PlayerPrefs.DeleteKey(InventoryKey);
            PlayerPrefs.Save();
            ApplyOverridesToLivePlayers();
        }

        public bool WasPressedThisFrame(MenuBindingId id)
        {
            InputAction action;
            int bindingIndex;
            if (!TryResolve(id, out action, out bindingIndex)) return false;
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

            CreateInventoryAction();
            savedInventoryPath = PlayerPrefs.GetString(InventoryKey, DefaultInventoryPath);
            inventoryAction.RemoveAllBindingOverrides();
            if (!string.Equals(savedInventoryPath, DefaultInventoryPath, StringComparison.OrdinalIgnoreCase))
                inventoryAction.ApplyBindingOverride(0, savedInventoryPath);
            ApplyOverridesToLivePlayers();
        }

        private void CreateInventoryAction()
        {
            if (inventoryAction != null) return;
            inventoryAction = new InputAction("Inventory", InputActionType.Button, DefaultInventoryPath);
            inventoryAction.Enable();
        }

        private bool TryResolve(MenuBindingId id, out InputAction action, out int bindingIndex)
        {
            if (id == MenuBindingId.Inventory)
            {
                CreateInventoryAction();
                action = inventoryAction;
                bindingIndex = 0;
                return true;
            }

            action = null;
            bindingIndex = -1;
            if (actions == null || !Targets.TryGetValue(id, out BindingTarget target)) return false;

            action = actions.FindAction(target.action, false);
            if (action == null) return false;

            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                bool keyboard = binding.path != null &&
                    binding.path.IndexOf("<Keyboard>", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!keyboard) continue;
                if (target.part == null ||
                    string.Equals(binding.name, target.part, StringComparison.OrdinalIgnoreCase))
                {
                    bindingIndex = i;
                    return true;
                }
            }

            return false;
        }

        private bool HasConflict(MenuBindingId changedId, string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            foreach (MenuBindingId id in Enum.GetValues(typeof(MenuBindingId)))
            {
                if (id == changedId) continue;
                InputAction otherAction;
                int otherIndex;
                if (!TryResolve(id, out otherAction, out otherIndex)) continue;
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
            if (action == inventoryAction && !action.enabled) action.Enable();
        }

        private void ApplyOverridesToLivePlayers()
        {
            if (actions == null) return;
            string json = actions.SaveBindingOverridesAsJson();
            foreach (PlayerInput playerInput in FindObjectsByType<PlayerInput>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None))
            {
                if (playerInput.actions == null || playerInput.actions == actions) continue;
                try { playerInput.actions.LoadBindingOverridesFromJson(json); }
                catch (Exception exception)
                {
                    Debug.LogWarning($"InputBindingService: could not update '{playerInput.name}'. {exception.Message}");
                }
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
            ApplyOverridesToLivePlayers();
        }
    }
}
