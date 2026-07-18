using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Splines.SplineInstantiate;
using Unity.Cinemachine;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [SerializeField] private CanvasGroup fadeScreen; // Link to a full-screen UI panel
    [SerializeField] private float fadeDuration = 2.5f;

    [SerializeField] private PlayerCamera mainCamera;
    [SerializeField] private CinemachineCamera virtualCamera;

    [SerializeField] private PlayerController playerController;


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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
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