using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Splines.SplineInstantiate;
using Unity.Cinemachine;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [SerializeField] private CanvasGroup fadeScreen; // Link to a full-screen UI panel
    [SerializeField] private float fadeDuration = 2.5f;

    [SerializeField] private PlayerCamera mainCamera;
    [SerializeField] private CinemachineCamera virtualCamera;

    [SerializeField] private PlayerController playerController;

    [Header("Persistent game UI")]
    [SerializeField] private GameObject menuOverlayPrefab;
    [SerializeField] private GameObject ingameStatPrefab;
    [SerializeField] private GameObject itemObtainedPrefab;

    private Game.UI.MenuTabController menuOverlay;
    private IngameStat ingameStat;
    private ItemObtained itemObtained;
    private Transform persistentUiRoot;


    public event Action<float> OnSavingLock;

    string currentScene = "null";




    private Vector3 offsetSpawn = Vector3.zero;

    private string targetSpawnPointId;

    float coolDownTransition = -1.0f;

    float maxcoolDownTransition = 2.0f;

    private void Awake()
    {
        // Ensure only one instance of this manager ever exists (Singleton)
        if (Instance == null)
        {
            Instance = this;
            SceneManager.sceneLoaded += SetRoom;

            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            RemoveSceneUiDuplicates();
            CreatePersistentUi();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RemoveSceneUiDuplicates();
        RebindPlayer();
    }

    private void CreatePersistentUi()
    {
        persistentUiRoot = CreateUiCanvas();
        if (menuOverlayPrefab != null)
        {
            GameObject overlayObject = Instantiate(menuOverlayPrefab, persistentUiRoot);
            StretchToParent(overlayObject.transform as RectTransform);
            menuOverlay = overlayObject.GetComponent<Game.UI.MenuTabController>();
            ConfigureSorting(overlayObject, 1000, true);
        }
        if (ingameStatPrefab != null)
        {
            GameObject statObject = Instantiate(ingameStatPrefab, persistentUiRoot);
            StretchToParent(statObject.transform as RectTransform);
            ingameStat = statObject.GetComponent<IngameStat>();
            if (ingameStat == null) ingameStat = statObject.AddComponent<IngameStat>();
            ConfigureSorting(statObject, -1000, false);
        }
        if (itemObtainedPrefab != null)
        {
            GameObject itemObject = Instantiate(itemObtainedPrefab, persistentUiRoot);
            StretchToParent(itemObject.transform as RectTransform);
            itemObtained = itemObject.GetComponent<ItemObtained>();
            if (itemObtained == null) itemObtained = itemObject.AddComponent<ItemObtained>();
            ConfigureSorting(itemObject, 2000, true);
        }
        RebindPlayer();
    }

    private static void ConfigureSorting(GameObject root, int sortingOrder, bool receivesInput)
    {
        // UnityEngine.Object has a special null state for unloaded/destroyed native
        // components. Null-coalescing does not use Unity's overloaded == operator and can
        // therefore return a fake-null Canvas. Use an explicit Unity null check here.
        Canvas canvas = root.GetComponent<Canvas>();
        if (canvas == null) canvas = root.AddComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"Unable to create a Canvas for persistent UI '{root.name}'.", root);
            return;
        }
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        foreach (Canvas childCanvas in root.GetComponentsInChildren<Canvas>(true))
        {
            if (childCanvas == canvas) continue;
            childCanvas.overrideSorting = false;
        }

        foreach (GraphicRaycaster raycaster in root.GetComponentsInChildren<GraphicRaycaster>(true))
            raycaster.enabled = receivesInput && raycaster.gameObject == root;

        GraphicRaycaster rootRaycaster = root.GetComponent<GraphicRaycaster>();
        if (receivesInput && rootRaycaster == null)
            rootRaycaster = root.AddComponent<GraphicRaycaster>();
        if (rootRaycaster != null) rootRaycaster.enabled = receivesInput;

        if (!receivesInput)
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
    }

    private static void StretchToParent(RectTransform rect)
    {
        if (rect == null) return;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private Transform CreateUiCanvas()
    {
        GameObject canvasObject = new GameObject("PersistentGameUI", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvasObject.transform;
    }

    private void RemoveSceneUiDuplicates()
    {
        // Older scenes still serialize visual-only copies of these prefabs. They do not
        // carry IngameStat/ItemObtained components (the manager adds those at runtime),
        // so component searches alone cannot find them.
        if (ingameStatPrefab != null || ingameStat != null)
            RemoveNamedSceneUi("Ingame_Stat", ingameStat != null ? ingameStat.gameObject : null);
        if (itemObtainedPrefab != null || itemObtained != null)
        {
            RemoveNamedSceneUi("Item_Obtained_UI", itemObtained != null ? itemObtained.gameObject : null);
            RemoveNamedSceneUi("Item_Obtained", itemObtained != null ? itemObtained.gameObject : null);
        }

        if (menuOverlayPrefab != null || menuOverlay != null)
        {
            foreach (Game.UI.MenuTabController candidate in FindObjectsByType<Game.UI.MenuTabController>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate == null || candidate == menuOverlay || IsPersistentUi(candidate.transform)) continue;
                candidate.gameObject.SetActive(false);
                Destroy(candidate.gameObject);
            }
        }

        if (ingameStatPrefab != null || ingameStat != null)
        {
            foreach (IngameStat candidate in FindObjectsByType<IngameStat>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate == null || candidate == ingameStat || IsPersistentUi(candidate.transform)) continue;
                candidate.gameObject.SetActive(false);
                Destroy(candidate.gameObject);
            }
        }

        if (itemObtainedPrefab != null || itemObtained != null)
        {
            foreach (ItemObtained candidate in FindObjectsByType<ItemObtained>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate == null || candidate == itemObtained || IsPersistentUi(candidate.transform)) continue;
                ItemObtained.ReleaseInstance(candidate);
                candidate.gameObject.SetActive(false);
                Destroy(candidate.gameObject);
            }
        }
    }

    private bool IsPersistentUi(Transform candidate)
    {
        return persistentUiRoot != null && candidate != null && candidate.IsChildOf(persistentUiRoot);
    }

    private void RemoveNamedSceneUi(string rootName, GameObject retained)
    {
        foreach (Transform candidate in FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate == null || candidate.gameObject == retained || IsPersistentUi(candidate)) continue;
            string candidateName = candidate.name.Replace("(Clone)", string.Empty).Trim();
            if (!string.Equals(candidateName, rootName, StringComparison.OrdinalIgnoreCase)) continue;

            ItemObtained popup = candidate.GetComponent<ItemObtained>();
            if (popup != null) ItemObtained.ReleaseInstance(popup);
            candidate.gameObject.SetActive(false);
            Destroy(candidate.gameObject);
        }
    }

    private void RebindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        playerController = player.GetComponent<PlayerController>();
        ingameStat?.Bind(player.GetComponent<PlayerStats>(), player.GetComponent<Inventory>());
    }

    public void OpenMenu(string tab = null)
    {
        if (menuOverlay == null) return;
        if (string.IsNullOrWhiteSpace(tab)) menuOverlay.OpenOverlay();
        else menuOverlay.OpenOverlayAtTab(tab);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    // This is the global function any door in the game can call

    private void Update()
    {
        coolDownTransition -= Time.deltaTime;
    }

    public void TransitionToScene(string buildSceneIndex, string spawnPointId, Vector3 _offsetSpawn)
    {
        if(coolDownTransition > 0)
        {
            return;
        }
        currentScene = buildSceneIndex;
        coolDownTransition = maxcoolDownTransition;

        Sfx.Play(SfxId.WorldTransition);

        targetSpawnPointId = spawnPointId;
        offsetSpawn = _offsetSpawn;

        playerController.RunForwardForXDistance(_offsetSpawn.x);
        StartCoroutine(LoadSceneRoutine(buildSceneIndex));
    }

    private void SetRoom(Scene scene, LoadSceneMode mode)
    {
        GameObject spawnPoint = GameObject.Find(targetSpawnPointId);
        if (spawnPoint)
        {
            Room room = spawnPoint.GetComponent<Room>();
            if (room != null)
            {
                room.usedThisDoor = true;
            }
        }
    }

    public IEnumerator ReloadScene()
    {
        
        Sfx.Play(SfxId.WorldTransition);

        targetSpawnPointId = "null";

        if(currentScene == "null")
        {
            currentScene = SceneManager.GetActiveScene().name;
        }

        yield return LoadSceneRoutine(currentScene, false);
    }





    private IEnumerator LoadSceneRoutine(string sceneName, bool Teleport = true)
    {

        bool alreadyFade = (fadeScreen.alpha > 0.0f);
        // 1. Fade out screen
        if(!alreadyFade)
           yield return StartCoroutine(Fade(1));

        // 2. Load the scene asynchronously in the background

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 3. Update Camera's Boundary
        UpdateCameraBoundary();

        // 4. Move player to the correct spawn point in the new scene
        if(Teleport)
            PositionPlayerAtSpawnPoint();

        // 5. Fade back in
        if(!alreadyFade)
             yield return StartCoroutine(Fade(0));
    }

    private void PositionPlayerAtSpawnPoint()
    {
        GameObject spawnPoint = GameObject.Find(targetSpawnPointId);
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (spawnPoint != null && player != null)
        {
            
            Vector3 finalPlayerPos = spawnPoint.transform.position + new Vector3(offsetSpawn.x * 1.15f, 0, 0);
           
            virtualCamera.ForceCameraPosition(finalPlayerPos, virtualCamera.transform.rotation);
            virtualCamera.PreviousStateIsValid = false;
           
           
        
            playerController.Teleport(spawnPoint.transform.position);
            playerController.RunForwardForXDistance(offsetSpawn.x);
        }
        else
        {
           Debug.Log("Wtf" + (player != null ? "Player" : "") + (spawnPoint != null ? "Point" : ""));
        }
    }
    public IEnumerator Fade(float targetAlpha)
    {
        float speed = 1f / fadeDuration;
        while (!Mathf.Approximately(fadeScreen.alpha, targetAlpha))
        {
            fadeScreen.alpha = Mathf.MoveTowards(fadeScreen.alpha, targetAlpha, speed * Time.deltaTime);
            yield return null;
        }
        
    }

    void UpdateCameraBoundary()
    {
        mainCamera.UpdateGlobalCameraBoundary();
    }
}
