using Game.UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public InputSystem_Actions Controls { get; private set; }
    public InputActionAsset SubControls { get; private set; }

    private InputAction moveAction;
    private InputAction lookAction;
    private InputActionMap playerMap;

    private PlayerInput playerInput;

    public Vector2 Move = Vector2.zero;
    public Vector2 Look  = Vector2.zero;

    public bool UpPress { get; private set; }
    public bool DownPress { get; private set; }
    public bool RightPress { get; private set; }
    public bool LeftPress { get; private set; }

    public bool CancelPress { get; private set; }
    public bool AcceptPress { get; private set; }
    public bool PreviousPress {  get; private set; }
    public bool NextPress { get; private set; }

    [SerializeField]
    GameObject playerObj;
    PlayerController player;




    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        player = playerObj.GetComponent<PlayerController>();

        

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        

    }

    private void Start()
    {
        var service = InputBindingService.Instance;

        if (service == null)
        {
            Debug.LogError("InputBindingService not found.");
            return;
        }

        Initialize(playerInput.actions);
    }

    public void Initialize(InputActionAsset actions)
    {
        SubControls = actions;

        playerMap = SubControls.FindActionMap("Player");

        moveAction = playerMap.FindAction("Move");
        lookAction = playerMap.FindAction("Look");

        moveAction.started += OnMove;
        moveAction.performed += OnMove;
        moveAction.canceled += OnMove;

        lookAction.started += OnLook;
        lookAction.performed += OnLook;
        lookAction.canceled += OnLook;

        //Debug.Log(playerInput.actions);
        Debug.Log(InputBindingService.Instance.actions);
        Debug.Log(playerInput.actions == InputBindingService.Instance.actions);
    }


    private void OnEnable()
    {
        if (Controls == null)
            return;


        playerMap.Enable();




    }

    private void OnDisable()
    {
        playerMap.Disable();
    }



    public void OnMove(InputAction.CallbackContext ctx)
    {
        Move = ctx.ReadValue<Vector2>();
        if (Mathf.Abs(Move.x) < 0.3)
        {
            Move.x = 0.0f;
        }

        if (Mathf.Abs(Move.y) < 0.6)
        {
            Move.y = 0.0f;
        }

        Move = NormalizeVectorToAllDirection(Move);

        if(Move.x > 0.0f)
        {
            RightPress = true;
        }
        if (Move.x < 0.0f)
        {
            LeftPress = true;
        }

        if(Move.y > 0.0f)
        {
            UpPress = true;
        }
        if(Move.y < 0.0f)
        {
            DownPress = true;
        }
    }

    public void OnCancel(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            CancelPress = true;
        }
    }


    public void OnPrevious(InputAction.CallbackContext ctx)
    {
        if(ctx.performed)
        {
            PreviousPress = true;
        }
    }

    
    public void OnNext(InputAction.CallbackContext ctx)
    {
        if(ctx.performed)
        {
            NextPress = true;
        }
    }

    private void LateUpdate()
    {
        UpPress = false;
        DownPress = false;
        RightPress = false;
        LeftPress = false;

        CancelPress = false;
        PreviousPress = false;
        NextPress = false;
    }


    private Vector2 NormalizeVectorToAllDirection(Vector2 input)
    {
        if (input == Vector2.zero) return Vector2.zero;

        if (Mathf.Abs(input.x) > 0.0f)
        {
            if (input.x < 0)
            {
                input.x = -1;
            }
            else
            {
                input.x = 1;
            }
        }
        if (Mathf.Abs(input.y) > 0.0f)
        {
            if (input.y < 0)
            {
                input.y = -1;
            }
            else
            {
                input.y = 1;
            }
        }
        return input;
    }




    public void OnLook(InputAction.CallbackContext ctx)
    {
        Look = ctx.ReadValue<Vector2>();
        
    }


    



    
}
