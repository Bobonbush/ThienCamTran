using System;
using UnityEngine;

public class CutSceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public static CutSceneManager Instance { get; private set; }
    public GameObject gameplayObject;
    public GameObject UIObject;

    [NonSerialized]
    public PlayerController playerController;
    [NonSerialized]
    public PlayerCamera playerCamera;

    private void Awake()
    {
        // Ensure only one instance of this manager ever exists (Singleton)
        if (Instance == null)
        {
            Instance = this;

            GameObject player = gameplayObject = GameObject.Find("Player");
            playerController = player.GetComponent<PlayerController>();
            playerCamera = player.GetComponentInChildren<PlayerCamera>();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

    }

    public void OnCutSceneStart(CutSceneInfo info)
    {
            
        // Turn off Hub

        if(gameplayObject == null)
        {
            gameplayObject = GameObject.Find("Gameplay");
        }
        if (gameplayObject != null)
        {
            
            if (info.useBigDialog)
            {
                gameplayObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("[CutScene] gameplayObject is MISSING/NULL in the inspector!");
        }

        if(UIObject != null && info.HideUI)
        {
            UIObject.SetActive(false);
        }
    }

    public void OnCutSceneEnd(CutSceneInfo info)
    {

        // Turn on Hub Again
        if (info.useBigDialog)
        {
            gameplayObject.SetActive(true);
        }

        if (UIObject != null)
        {
            UIObject.SetActive(true);
        }

        if (info.newScene && info.HideUI)
        {
            SceneTransitionManager sceneManager = SceneTransitionManager.Instance;
            sceneManager.TransitionToScene(info.SceneID, info.SpawnID, info.SpawnOffset);
        }
    }
}
