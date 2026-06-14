using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Damageable))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpImpulse = 10f;
    public float airWalkSpeed = 3f;

    public float dashSpeed = 200f;
    public float dashCooldown = 0.8f;

    private float lastDashTime = -999f;
    private int dashDir = 1;
    private bool canAirDash = true;
    private bool wasDashing = false;

    PlayerStats stat;

    Vector2 moveInput;
    TouchingDirections touchingDirections;
    Damageable damageable;

    Vector3 SafeGround = Vector3.zero;
    float LastOnGroundY = 0;

    public float CurrentSpeed
    {
        get
        {
            if (CanMove)
            {
                if (IsMoving && !touchingDirections.IsOnWall)
                {
                    if (touchingDirections.IsGrounded)
                    {
                        if (IsRunning)
                        {
                            return runSpeed;

                        }
                        else
                        {
                            return walkSpeed;
                        }
                    }
                    else
                    {
                        return Mathf.Max(walkSpeed, Mathf.Abs(rb.linearVelocityX));
                    }
                }
                else
                {
                    
                    return 0;
                }
            }
            else
            {

            }
            {
                // no movement allowed when can not move
                return 0;
            }
            
        }
    }

    public bool IsDashing
    {
        get
        {
            return animator.GetBool(AnimationStrings.isDashing);
        }
    }

    [SerializeField]
    private bool _isMoving = false;
    public bool IsMoving { get
        {
            return _isMoving;
        }
        private set
        {
            _isMoving = value;
            animator.SetBool(AnimationStrings.isMoving, value);
        }
    }

    [SerializeField]
    private bool _isRunning = false;

    public bool IsRunning
    {
        get
        {
            return _isRunning;
        }
        private set
        {
            _isRunning = value;
            animator.SetBool(AnimationStrings.isRunning, value);
        }
    }

    public bool _isFacingRight = true;

    public Vector2 lookInput { get; private set; }

    public bool IsFacingRight { 
        get 
        { 
            return _isFacingRight; 
        } 
        private set
        {
            if (_isFacingRight != value)
            {
                _isFacingRight = value;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1;
                transform.localScale = localScale;
            }
        }
    }

    public bool CanMove
    {
        get
        {
            return animator.GetBool(AnimationStrings.canMove);
        }

    }

    public bool IsAlive
    {
        get
        {
            return animator.GetBool(AnimationStrings.isAlive);
        }
    }

    public bool IsIdle
    {
        get
        {
            return Mathf.Abs(rb.linearVelocity.x) < 0.1f && Mathf.Abs(rb.linearVelocity.y) < 0.1f;
        }
    }



    

    Rigidbody2D rb;
    Animator animator;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        touchingDirections = GetComponent<TouchingDirections>();
        damageable = GetComponent<Damageable>();
        stat = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if(damageable.ReviveAction)
        {
            damageable.ReviveAction = false;
            Revive();
        }

        if(touchingDirections.IsGrounded)
        {
            LastOnGroundY = transform.position.y;
            canAirDash = true;
        }
    }

    private void FixedUpdate()
    {
        if (IsDashing)
        {
            //rb.AddForce();
            rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
        }
        else 
        {
            if (wasDashing)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (!damageable.LockVelocity)
                rb.linearVelocity = new Vector2(moveInput.x * CurrentSpeed, rb.linearVelocity.y);
        }

        wasDashing = IsDashing;
        animator.SetFloat(AnimationStrings.yVelocity, rb.linearVelocity.y);
    }

    public void SetFacingDirection(Vector2 moveInput)
    {
        if (moveInput.x > 0 && !IsFacingRight)
        {
            IsFacingRight = true;
        }
        else if (moveInput.x < 0 && IsFacingRight)
        {
            IsFacingRight = false;
        }
    }

    
    public void OnLook(InputAction.CallbackContext context)
    {
        if (touchingDirections.IsGrounded)
        {
            lookInput = context.ReadValue<Vector2>();
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        if (IsAlive)
        {
            IsMoving = moveInput != Vector2.zero;
            SetFacingDirection(moveInput);
        }
        else
        {
            IsMoving = false;
        }

        
    }

    
    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            IsRunning = true;
        }
        else if (context.canceled)
        {
            IsRunning = false;
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        // TODO Check if alive as well
        if (context.started && touchingDirections.IsGrounded && CanMove)
        {
            animator.SetTrigger(AnimationStrings.jumpTrigger);   
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpImpulse);
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("Attack pressed");
            animator.SetTrigger(AnimationStrings.attackTrigger);
        }
    }

    public void OnRangedAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("Ranged attack pressed");
            animator.SetTrigger(AnimationStrings.rangedAttackTrigger);
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started && CanDash())
        {
            if (!touchingDirections.IsGrounded)
                canAirDash = false;     // used air-dash

            lastDashTime = Time.time;
            dashDir = IsFacingRight ? 1 : -1;
            animator.SetTrigger(AnimationStrings.dashTrigger);
        }
    }

    public void OnSlot1(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("First Slot pressed");
            stat.TriggerSlot1(animator);
        }
    }

    public void OnSlot2(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("Second Slot pressed");
            stat.TriggerSlot2(animator);
        }
    }

    private bool CanDash()
    {
        if (!IsAlive || !CanMove || IsDashing)
            return false;
        if (Time.time < lastDashTime + dashCooldown)
            return false;

        return touchingDirections.IsGrounded || canAirDash;
    }


    public void OnHit(int damage, Vector2 knockback)
    {
        rb.linearVelocity = new Vector2(knockback.x, rb.linearVelocity.y + knockback.y);
    }
    

    public void OnHeal(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            stat.Heal();
        }
    }

    // When fall out of map or fall into traps
    private void Revive() 
    {
        Debug.Log("Revive!");
        transform.position = SafeGround;
    }

    public void SetSafeGround(Vector3 position)
    {
        SafeGround = position;
        SafeGround.y = LastOnGroundY;
    }
}
