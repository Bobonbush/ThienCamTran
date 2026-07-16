using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CheckpointShrineBuilder
{
    private const string SavePrefabPath = "Assets/Objects/SavePlace/Save.prefab";
    private const string TestScenePath = "Assets/Scenes/UI/CheckpointTest.unity";
    private const string PlayerPrefabPath = "Assets/Character/Player/Player.prefab";

    [MenuItem("Tools/Game2D/Rebuild Checkpoint Shrine UI")]
    public static void BuildAll()
    {
        BuildSavePrefab();
        BuildTestScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Checkpoint shrine UI and test scene rebuilt.");
    }

    private static void BuildSavePrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(SavePrefabPath);
        try
        {
            Transform oldUi = root.transform.Find("CheckpointWorldUI");
            if (oldUi != null)
                Object.DestroyImmediate(oldUi.gameObject);

            Sprite panelSprite = LoadSprite("Assets/UI/Common_UI/Menu_Panel.aseprite");
            Sprite selectedSprite = LoadSprite("Assets/UI/Common_UI/Button_Selected_Medium.aseprite");
            Sprite unselectedSprite = LoadSprite("Assets/UI/Common_UI/Button_Unselected_Medium.aseprite");
            Sprite roundSprite = LoadSprite("Assets/UI/Common_UI/Button_Round.aseprite");
            Sprite ribbonSprite = LoadSprite("Assets/UI/Common_UI/Game_save_ribbon.aseprite");
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/UI/Fonts/m5x7 SDF.asset"
            );

            GameObject canvasObject = CreateUIObject("CheckpointWorldUI", root.transform);
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(320f, 240f);
            canvasRect.localPosition = new Vector3(2.8f, 2.2f, 0f);
            canvasRect.localScale = Vector3.one * 0.01f;

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;
            canvasObject.AddComponent<GraphicRaycaster>();
            CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();

            GameObject panel = CreateImage("MenuPanel", canvasRect, panelSprite, new Vector2(250f, 185f));
            SetAnchoredPosition(panel, Vector2.zero);
            CreateText("Title", panel.transform, "CHECKPOINT", font, 22f,
                new Vector2(190f, 28f), new Vector2(0f, 68f), TextAlignmentOptions.Center);

            Button rest = CreateOptionButton("RestButton", panel.transform, "REST", font,
                unselectedSprite, new Vector2(150f, 34f), new Vector2(-5f, 30f));
            Button status = CreateOptionButton("StatusButton", panel.transform, "STATUS", font,
                unselectedSprite, new Vector2(150f, 34f), new Vector2(-5f, -14f));
            Button cancel = CreateOptionButton("CancelButton", panel.transform, "CANCEL", font,
                unselectedSprite, new Vector2(150f, 34f), new Vector2(-5f, -58f));

            GameObject saving = CreateImage("GameSavingNoti", canvasRect, ribbonSprite,
                new Vector2(175f, 30f));
            SetAnchoredPosition(saving, new Vector2(0f, 108f));
            TMP_Text savingText = CreateText("SavingText", saving.transform, "Game is saving.", font,
                13f, new Vector2(155f, 24f), Vector2.zero, TextAlignmentOptions.Center);

            GameObject instructions = CreateUIObject("ControlInstructions", canvasRect);
            RectTransform instructionsRect = instructions.GetComponent<RectTransform>();
            instructionsRect.sizeDelta = new Vector2(205f, 28f);
            instructionsRect.anchoredPosition = new Vector2(55f, -108f);
            CreateInstruction(instructions.transform, "Confirm", "E", "Confirm", font, roundSprite,
                new Vector2(-47f, 0f));
            CreateInstruction(instructions.transform, "Cancel", "Q", "Cancel", font, roundSprite,
                new Vector2(55f, 0f));

            CheckpointWorldUI worldUi = canvasObject.AddComponent<CheckpointWorldUI>();
            SerializedObject uiData = new SerializedObject(worldUi);
            uiData.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            uiData.FindProperty("contentRoot").objectReferenceValue = canvasRect;
            uiData.FindProperty("restButton").objectReferenceValue = rest;
            uiData.FindProperty("statusButton").objectReferenceValue = status;
            uiData.FindProperty("cancelButton").objectReferenceValue = cancel;
            uiData.FindProperty("selectedSprite").objectReferenceValue = selectedSprite;
            uiData.FindProperty("unselectedSprite").objectReferenceValue = unselectedSprite;
            uiData.FindProperty("savingNotification").objectReferenceValue = saving;
            uiData.FindProperty("savingText").objectReferenceValue = savingText;
            uiData.ApplyModifiedPropertiesWithoutUndo();

            SaveZone saveZone = root.GetComponentInChildren<SaveZone>(true);
            if (saveZone == null)
                throw new MissingComponentException("Save prefab has no SaveZone component.");

            SerializedObject saveData = new SerializedObject(saveZone);
            saveData.FindProperty("checkpointUI").objectReferenceValue = worldUi;
            saveData.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, SavePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void BuildTestScene()
    {
        Scene scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);

        GameObject oldShrine = GameObject.Find("CheckpointShrine");
        if (oldShrine != null)
            Object.DestroyImmediate(oldShrine);

        GameObject savePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SavePrefabPath);
        GameObject shrine = (GameObject)PrefabUtility.InstantiatePrefab(savePrefab, scene);
        shrine.name = "CheckpointShrine";
        shrine.transform.position = Vector3.zero;

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject playerObject = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            playerObject.name = "Player";
            playerObject.transform.position = new Vector3(-3f, -1.5f, 0f);
        }

        GameObject oldGround = GameObject.Find("CheckpointTestGround");
        if (oldGround != null)
            Object.DestroyImmediate(oldGround);

        GameObject ground = new GameObject("CheckpointTestGround");
        SceneManager.MoveGameObjectToScene(ground, scene);
        ground.layer = LayerMask.NameToLayer("Ground");
        ground.transform.position = new Vector3(0f, -2.6f, 0f);
        BoxCollider2D groundCollider = ground.AddComponent<BoxCollider2D>();
        groundCollider.size = new Vector2(20f, 1f);

        CheckpointUITest test = Object.FindFirstObjectByType<CheckpointUITest>();
        if (test != null)
        {
            SerializedObject testData = new SerializedObject(test);
            testData.FindProperty("checkpointUI").objectReferenceValue =
                shrine.GetComponentInChildren<CheckpointWorldUI>(true);
            testData.ApplyModifiedPropertiesWithoutUndo();
        }

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            SceneManager.MoveGameObjectToScene(eventSystem, scene);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Button CreateOptionButton(string name, Transform parent, string label,
        TMP_FontAsset font, Sprite sprite, Vector2 size, Vector2 position)
    {
        GameObject buttonObject = CreateImage(name, parent, sprite, size);
        SetAnchoredPosition(buttonObject, position);
        Image image = buttonObject.GetComponent<Image>();
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        CreateText("Label", buttonObject.transform, label, font, 17f,
            size - new Vector2(16f, 4f), Vector2.zero, TextAlignmentOptions.Center);
        return button;
    }

    private static void CreateInstruction(Transform parent, string name, string key, string command,
        TMP_FontAsset font, Sprite roundSprite, Vector2 position)
    {
        GameObject root = CreateUIObject(name, parent);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(95f, 24f);
        rootRect.anchoredPosition = position;

        GameObject icon = CreateImage("Button", rootRect, roundSprite, new Vector2(22f, 22f));
        SetAnchoredPosition(icon, new Vector2(-34f, 0f));
        CreateText("Key", icon.transform, key, font, 12f,
            new Vector2(20f, 20f), Vector2.zero, TextAlignmentOptions.Center);
        CreateText("Command", root.transform, command, font, 12f,
            new Vector2(65f, 22f), new Vector2(12f, 0f), TextAlignmentOptions.Left);
    }

    private static GameObject CreateImage(string name, Transform parent, Sprite sprite, Vector2 size)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = false;
        return go;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, TMP_FontAsset font,
        float fontSize, Vector2 size, Vector2 position, TextAlignmentOptions alignment)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color32(40, 27, 22, 255);
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetAnchoredPosition(GameObject gameObject, Vector2 position)
    {
        gameObject.GetComponent<RectTransform>().anchoredPosition = position;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (sprite == null)
            throw new MissingReferenceException($"No Sprite sub-asset found at '{path}'.");
        return sprite;
    }
}
