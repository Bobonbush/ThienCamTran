#if UNITY_EDITOR
using System.Linq;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public static class MainMenuSceneSetup
{
    private const string ScenePath = "Assets/Scenes/UI/MainMenuNew.unity";

    [InitializeOnLoadMethod]
    private static void QueueAutomaticSetup()
    {
        EditorApplication.delayCall += ConfigureMainMenu;
    }

    [MenuItem("Tools/Game UI/Configure Main Menu")]
    public static void ConfigureMainMenu()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
        if (openedForSetup)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        Canvas canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(value => value.gameObject.scene == scene);
        if (canvas == null)
        {
            Debug.LogError("MainMenuSceneSetup: Canvas was not found.");
            if (openedForSetup) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        MainMenuController controller = canvas.GetComponent<MainMenuController>();
        if (controller == null) controller = canvas.gameObject.AddComponent<MainMenuController>();

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("selectedButtonSprite").objectReferenceValue =
            LoadSprite("Assets/UI/Common_UI/Button/Button_Selected_Medium.aseprite");

        string[] iconPaths =
        {
            "icon_book_red", "icon_candy", "icon_cracker", "icon_egg", "icon_feather",
            "icon_fish", "icon_leaf", "icon_meat", "icon_wood", "Lore_Icon", "Point_Icon"
        };
        SerializedProperty icons = serialized.FindProperty("selectableIcons");
        icons.arraySize = iconPaths.Length;
        for (int i = 0; i < iconPaths.Length; i++)
            icons.GetArrayElementAtIndex(i).objectReferenceValue =
                LoadSprite($"Assets/UI/Common_UI/Icon/{iconPaths[i]}.aseprite");

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
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (openedForSetup) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("MainMenuSceneSetup: MainMenuNew is configured.");
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }
}
#endif
