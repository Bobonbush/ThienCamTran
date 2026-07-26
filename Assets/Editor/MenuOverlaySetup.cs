#if UNITY_EDITOR
using System.Linq;
using Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

/// <summary>
/// Wires the Setting tab of the in-game book overlay, mirroring <see cref="MainMenuSceneSetup"/>.
/// Swaps the placeholder EmptyPanel on SettingPanel for <see cref="SettingsMenuPanel"/> and assigns
/// the same artwork/services the main menu uses, so the two settings screens stay identical.
///
/// Runs automatically, but ONLY while SettingPanel still carries the placeholder — once the swap
/// has happened this never touches the prefab again. That keeps the one-time migration from being
/// a step someone has to remember, without the save-on-every-editor-load churn MainMenuSceneSetup
/// causes.
/// </summary>
public static class MenuOverlaySetup
{
    private const string PrefabPath = "Assets/Prefabs/MenuOverlay.prefab";
    private const string OptionPanelPath = "Assets/Prefabs/Option_Panel .prefab";

    [InitializeOnLoadMethod]
    private static void QueueAutomaticSetup()
    {
        EditorApplication.delayCall += AutoConfigureIfNeeded;
    }

    private static void AutoConfigureIfNeeded()
    {
        // Never rewrite an asset mid-playtest; the next domain reload will pick it up.
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
            return;

        // Cheap check straight off the asset — no LoadPrefabContents, no write.
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (asset == null)
            return;

        if (asset.GetComponentInChildren<SettingsMenuPanel>(true) != null)
            return;

        ConfigureMenuOverlay();
    }

    [MenuItem("Tools/Game UI/Configure Menu Overlay")]
    public static void ConfigureMenuOverlay()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogError($"MenuOverlaySetup: could not load '{PrefabPath}'.");
            return;
        }

        try
        {
            Transform settingPanel = UIRuntime.Find(root.transform, "SettingPanel");
            if (settingPanel == null)
            {
                Debug.LogError("MenuOverlaySetup: 'SettingPanel' was not found in MenuOverlay.");
                return;
            }

            SettingsMenuPanel panel = ReplacePlaceholder(settingPanel.gameObject);

            SerializedObject serialized = new SerializedObject(panel);
            serialized.FindProperty("optionPanelPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(OptionPanelPath);
            serialized.FindProperty("selectedButtonSprite").objectReferenceValue =
                LoadSprite("Assets/UI/Common_UI/Button/Button_Selected_Medium.aseprite");
            serialized.FindProperty("videoLabelSprite").objectReferenceValue =
                LoadSprite("Assets/UI/Aseprite/TabBar/Tab_Label/Video_Label.aseprite");
            serialized.FindProperty("audioLabelSprite").objectReferenceValue =
                LoadSprite("Assets/UI/Aseprite/TabBar/Tab_Label/Audio_Label.aseprite");
            serialized.FindProperty("controlLabelSprite").objectReferenceValue =
                LoadSprite("Assets/UI/Aseprite/TabBar/Tab_Label/Control_Label.aseprite");
            serialized.FindProperty("audioMixer").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/MainMixer.mixer");
            serialized.FindProperty("inputActions").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            serialized.ApplyModifiedPropertiesWithoutUndo();

            RepointTabController(root, panel);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("MenuOverlaySetup: MenuOverlay Setting tab is configured.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>Replaces the placeholder EmptyPanel with the real settings panel, keeping any
    /// SettingsMenuPanel that a previous run already added.</summary>
    private static SettingsMenuPanel ReplacePlaceholder(GameObject settingPanel)
    {
        foreach (EmptyPanel placeholder in settingPanel.GetComponents<EmptyPanel>())
            Object.DestroyImmediate(placeholder, true);

        SettingsMenuPanel panel = settingPanel.GetComponent<SettingsMenuPanel>();
        if (panel == null) panel = settingPanel.AddComponent<SettingsMenuPanel>();
        return panel;
    }

    /// <summary>
    /// MenuTabController.panels holds a MenuPanel reference that pointed at the destroyed
    /// EmptyPanel, so the slot has to be re-aimed at the new component or the tab opens nothing.
    /// </summary>
    private static void RepointTabController(GameObject root, SettingsMenuPanel panel)
    {
        MenuTabController controller = root.GetComponentInChildren<MenuTabController>(true);
        if (controller == null)
        {
            Debug.LogWarning("MenuOverlaySetup: no MenuTabController found; panels array not updated.");
            return;
        }

        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty panels = serialized.FindProperty("panels");
        for (int i = 0; i < panels.arraySize; i++)
        {
            SerializedProperty element = panels.GetArrayElementAtIndex(i);
            Object current = element.objectReferenceValue;
            // The destroyed EmptyPanel leaves a null hole, or the slot already points at our panel.
            if (current == null || current is SettingsMenuPanel)
            {
                element.objectReferenceValue = panel;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return;
            }
        }

        Debug.LogWarning(
            "MenuOverlaySetup: no free slot in MenuTabController.panels — " +
            "assign SettingPanel manually in the inspector.");
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }
}
#endif
