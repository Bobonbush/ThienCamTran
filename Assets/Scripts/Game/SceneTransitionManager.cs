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

    private Vector3 offsetSpawn = Vector3.zero;

    private string targetSpawnPointId;

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
   

    public void TransitionToScene(string buildSceneIndex, string spawnPointId, Vector3 _offsetSpawn)
    {
        targetSpawnPointId = spawnPointId;
        offsetSpawn = _offsetSpawn;
        StartCoroutine(LoadSceneRoutine(buildSceneIndex));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        // 1. Fade out screen
        yield return StartCoroutine(Fade(1));

        // 2. Load the scene asynchronously in the background
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 3. Move player to the correct spawn point in the new scene
        PositionPlayerAtSpawnPoint();

        // 4. Get the camera boundary

        UpdateCameraBoundary();

        // 5. Fade back in
        yield return StartCoroutine(Fade(0));
    }

    private void PositionPlayerAtSpawnPoint()
    {
        // Find the spawn point in the newly loaded scene that matches our ID
        GameObject spawnPoint = GameObject.Find(targetSpawnPointId);
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (spawnPoint != null && player != null)
        {
            player.transform.position = spawnPoint.transform.position + offsetSpawn;
        }
        else
        {
            Debug.Log("Wtf" + (player != null ? "Player" : "") + (spawnPoint != null ? "Point" : ""));
        }
    }

    private IEnumerator Fade(float targetAlpha)
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
        // Find the bounds object in the newly loaded scene
        GameObject localBounds = GameObject.Find("CameraBounds");


        if (localBounds != null)
        {
            CinemachineConfiner2D confiner = GetComponentInChildren<CinemachineConfiner2D>();
            Collider2D targetCollider = localBounds.GetComponent<Collider2D>();


            confiner.BoundingShape2D = targetCollider;
            confiner.InvalidateBoundingShapeCache(); 
        }
    }
}