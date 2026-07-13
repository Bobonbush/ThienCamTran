using UnityEngine;

public class InteractiveRoom : MonoBehaviour, IInteractable
{

    [Header("Prompt")]
    public GameObject promptObject;

    [SerializeField]
    private string LinkedRoom = "OutSkirt-1";

    [SerializeField]
    private string SpawnPointId = "Outskirt-1-Top";

    [SerializeField]
    private Vector2 spawnOffset = new Vector2(0, 0);

    private Sealed seal = null;


    WorldSpacePrompt promp;


    [SerializeField]

    public bool Closed = false;


    public bool CanInteract
    {
        get { return promp == null || promp.FinishAnimation(); }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Awake()
    {
        promp = GetComponentInChildren<WorldSpacePrompt>();
        seal = GetComponent<Sealed>();
        SetPromptVisible(false);
    }
    void Start()
    {
        SetPromptVisible(false);
    }

    public void SetPromptVisible(bool visible)
    {
        bool canShow = visible && CanInteract;
        if (promptObject != null && promptObject.activeSelf != canShow)
            promptObject.SetActive(canShow);
    }

    public void Interact(PlayerController player)
    {
        if (!CanInteract )
            return;

        Debug.Log(seal != null);
        if(seal != null && seal.isSealed())
        {
            if (promp != null)
            {
                promp.OutSideActivate();
            }
            return;
        }
        EnterNextRoom();   
    }

    public void EnterNextRoom()
    {
        SceneTransitionManager.Instance.TransitionToScene(LinkedRoom, SpawnPointId, spawnOffset);
    }

    public new IInteractable.Type GetType()
    {
        return IInteractable.Type.Door;
    }


}
