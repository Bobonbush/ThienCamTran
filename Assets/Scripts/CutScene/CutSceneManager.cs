using UnityEngine;

public class CutSceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public static CutSceneManager Instance { get; private set; }
    public GameObject gameplayObject;

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

    public void OnCutSceneStart(CutSceneInfo info)
    {
            
        if (!info.RealTimeAnimation)
        {
            if(gameplayObject == null)
            {
                gameplayObject = GameObject.Find("Gameplay");
            }
            if (gameplayObject != null)
            {
                gameplayObject.SetActive(false);
                Debug.Log("[CutScene] gameplayObject hidden successfully.");
            }
            else
            {
                Debug.LogError("[CutScene] gameplayObject is MISSING/NULL in the inspector!");
            }
        }
    }

    public void OnCutSceneEnd(CutSceneInfo info)
    {
        if(!info.RealTimeAnimation)
        {
            gameplayObject.SetActive(true);
        }

        if(info.newScene)
        {
            SceneTransitionManager sceneManager = SceneTransitionManager.Instance;
            sceneManager.TransitionToScene(info.SceneID, info.SpawnID, info.SpawnOffset);

            Debug.Log(info.SceneID);
            Debug.Log(info.SpawnID);
        }
    }
}
