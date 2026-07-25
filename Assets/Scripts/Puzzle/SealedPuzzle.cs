using UnityEngine;

public class SealedPuzzle : MonoBehaviour, IInteractable
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private static bool firstEncounter = false;

    [Header("Prompt")]
    public GameObject promptObject;

    [SerializeField] private Transform left;
    [SerializeField] private Transform right;

    DialogInteractable dialogTrigger;

    private ActivateTrap activateTrap;

    private CutTrigger cutTrigger;
    private bool _solve = false;
    public bool Solved { get { return _solve; } }


    [SerializeField]
    private int _round = 3;

    public int Round {  get { return _round; } }

    
    private float _duration = 1.75f;

    public float Duration { get { return _duration; } }

    private EnemySpawn e_spawn;
    

    string saveID = string.Empty;

    [SerializeField] private string secretID = string.Empty;

    private bool useSecretID = false;

    public void Puzzle(PlayerController player)
    {
         player.EnterPuzzle(left.position, right.position, this);
    }

    private void Awake()
    {
        e_spawn = GetComponentInParent<EnemySpawn>();
        activateTrap = GetComponentInParent<ActivateTrap>();
        cutTrigger = GetComponentInParent<CutTrigger>();

        saveID = SaveIdUtility.For(this);
        useSecretID = (secretID != string.Empty && secretID != "");
        Debug.Log("Save ID for puzzle : " + saveID);
        dialogTrigger = GetComponent<DialogInteractable>();
    }

    void Start()
    {
        SetPromptVisible(false);


        if (SaveManager.Instance.IsPuzzleSolved(saveID) || (SaveManager.Instance.IsPuzzleSolved(secretID) && useSecretID))
        {
            _solve = true;
            if (activateTrap != null)
            {
                activateTrap.TriggerAnimation();
                activateTrap.InstantActivateTraps();
                if(e_spawn != null)
                {
                    e_spawn.ActiveAllTrap();
                }
            }
        }
    }

    public void SetPromptVisible(bool visible)
    {
        bool canShow = visible && CanInteract;
        if (promptObject != null && promptObject.activeSelf != canShow)
            promptObject.SetActive(canShow);
    }

    public bool CanInteract { get { return !Solved; } }
    
    public void Interact(PlayerController player)
    {
        if (!CanInteract || collisionCnt == 0 ) return;
        if(firstEncounter == false)
        {
            dialogTrigger.TriggerInteract();
            firstEncounter = true;
            return;
        }
        if(!player.isPuzzling())
        {
            Puzzle(player);
        }
    }

    
    public void Done(PlayerController playerController, PlayerCamera player)
    {
        _solve = true;
        if (!useSecretID)
        {
            SaveManager.Instance.MarkPuzzleSolved(saveID);
        }else
        {
            SaveManager.Instance.MarkPuzzleSolved(secretID);
        }
        if (cutTrigger != null)
            cutTrigger.Trigger(playerController, player);
        activateTrap.TriggerAnimation();
        activateTrap.ActivateTraps();

        if(e_spawn != null)
        {
            e_spawn.StartUp();
            e_spawn.SetUpCutScene(playerController, player);
        }
    }


    public new IInteractable.Type GetType()
    {
        return IInteractable.Type.Purifying;
    }

    private int collisionCnt = 0;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            collisionCnt++;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            collisionCnt--;
        }
    }

}
