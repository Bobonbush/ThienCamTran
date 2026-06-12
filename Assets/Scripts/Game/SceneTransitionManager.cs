using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [SerializeField] private CanvasGroup fadeScreen; // Link to a full-screen UI panel
    [SerializeField] private float fadeDuration = 0.5f;

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
    public void TransitionToScene(string sceneName, string spawnPointId)
    {
        targetSpawnPointId = spawnPointId;
        StartCoroutine(LoadSceneRoutine(sceneName));
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

        // 4. Fade back in
        yield return StartCoroutine(Fade(0));
    }

    private void PositionPlayerAtSpawnPoint()
    {
        // Find the spawn point in the newly loaded scene that matches our ID
        GameObject spawnPoint = GameObject.Find(targetSpawnPointId);
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (spawnPoint != null && player != null)
        {
            player.transform.position = spawnPoint.transform.position;
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
}