using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public InputSystem_Actions Controls { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created

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
        Controls = new InputSystem_Actions();

        // The generated wrapper builds its own asset from baked-in defaults, so it never sees the
        // rebinds applied to the PlayerInput asset unless it is registered as a mirror.
        Game.UI.InputBindingService.EnsureExists(null)?.RegisterMirror(Controls.asset);

        // Unity calls OnEnable itself right after Awake - calling it here too would double-subscribe.
    }


    private void OnEnable()
    {
        if (Controls == null) return;

        Controls.Enable();

        Controls.Player.Move.started += OnMove;
        Controls.Player.Move.performed += OnMove;
        Controls.Player.Move.canceled += OnMove;

        Controls.Player.Look.performed += OnLook;
        Controls.Player.Look.canceled += OnLook;
        Controls.Player.Look.started += OnLook;




    }

    private void OnDisable()
    {
        if (Controls == null) return;

        Controls.Player.Move.started -= OnMove;
        Controls.Player.Move.performed -= OnMove;
        Controls.Player.Move.canceled -= OnMove;

        Controls.Player.Look.performed -= OnLook;
        Controls.Player.Look.canceled -= OnLook;
        Controls.Player.Look.started -= OnLook;

        Controls.Disable();
    }

    private void OnDestroy()
    {
        if (Instance != this || Controls == null) return;
        Game.UI.InputBindingService.Instance?.UnregisterMirror(Controls.asset);
        Instance = null;
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


    



    public void EnablePlayer()
    {
        Controls.Player.Enable();
    }

    public void DisablePlayer()
    {
        Controls.Player.Disable();
    }

    public void EnableUI()
    {
        Controls.UI.Enable();
    }

    public void DisableUI()
    {
        Controls.UI.Disable();
    }
}
