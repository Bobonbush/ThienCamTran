using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Damageable))]
public class PlayerController : MonoBehaviour
{

    [SerializeField]
    private float maxComboTime = 0.3f;

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

    private bool lockInput = false;

    private float TargetMoveX = 0.0f;
    
    private Vector3 oldTransformPosition = Vector3.zero;
    private Vector2 lockDirection = new Vector2(1, 0);

    PlayerStats stat;

    Vector2 moveInput;
    TouchingDirections touchingDirections;
    Damageable damageable;

    Vector3 SafeGround = Vector3.zero;
    float LastOnGroundY = 0;
    float gravityScale = 0.0f;



    

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



    CapsuleCollider2D col;
    Rigidbody2D rb;
    Animator animator;

    private bool Climbing = false;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        touchingDirections = GetComponent<TouchingDirections>();
        damageable = GetComponent<Damageable>();
        stat = GetComponent<PlayerStats>();
        col = GetComponent<CapsuleCollider2D>();
        gravityScale = rb.gravityScale;
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

        
        if(lockInput )
        {
            rb.linearVelocity = new Vector2(Mathf.Max(CurrentSpeed, rb.linearVelocityX, walkSpeed)  , rb.linearVelocity.y) * lockDirection;
            TargetMoveX -= Mathf.Abs(transform.position.x - oldTransformPosition.x) ;
            oldTransformPosition = transform.position;
            if(TargetMoveX <= 0)
            {
                IsMoving = false;
                IsRunning = false;
                lockInput = false;
            }
            return ;
        }
        if (IsDashing)
        {
            rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
        }
        else 
        {
            if (wasDashing)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (!damageable.LockVelocity)
            {
                if (Climbing == false)
                    rb.linearVelocity = new Vector2(moveInput.x * CurrentSpeed, rb.linearVelocity.y);
                else
                {
                    
                    rb.linearVelocity = new Vector2(0.0f, lookInput.y * walkSpeed);
                    rb.gravityScale = 0.0f;
                    if(touchingDirections.canClimb == false) {
                        Climbing = false;
                        rb.gravityScale = gravityScale;
                    }
                }
            }
        }

        wasDashing = IsDashing;
        animator.SetFloat(AnimationStrings.yVelocity, rb.linearVelocity.y);

        oldTransformPosition = transform.position;
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


    private float duckLastTime = 0.0f;
    
    public void OnLook(InputAction.CallbackContext context)
    {
        if (lockInput) return;
        lookInput = context.ReadValue<Vector2>();

        if (lookInput != Vector2.zero)
        {

            if (Climbing == false)
            {
                OnClimb();
            }
        }

        


        if (touchingDirections.IsGrounded)
        {
            
            if (lookInput.y < 0f && Climbing == false)
            {
                duckLastTime = Time.time;
            }
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (lockInput) return;
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
        if (lockInput) return;

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
        if (lockInput) return;

        if (context.started && (touchingDirections.IsGrounded || Climbing) && CanMove)
        {
            Climbing = false;
            rb.gravityScale = gravityScale;

            if (Time.time - duckLastTime <= maxComboTime && touchingDirections.IsOnSlidable)
            {

                OnSlide();
                return;
            }

            
            animator.SetTrigger(AnimationStrings.jumpTrigger);   
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpImpulse);
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (lockInput) return;
        if (Climbing) return;
        if (context.started)
        {
            Debug.Log("Attack pressed");
            animator.SetTrigger(AnimationStrings.attackTrigger);
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (lockInput) return;
        if(Climbing) return;

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
        if (lockInput) return;
        if (Climbing) return;
        if (context.started)
        {
            Debug.Log("First Slot pressed");
            stat.TriggerSlot1(animator);
        }
    }

    public void OnSlot2(InputAction.CallbackContext context)
    {
        if (lockInput) return;
        if (Climbing) return;

        if (context.started)
        {
            Debug.Log("Second Slot pressed");
            stat.TriggerSlot2(animator);
        }
    }

    private bool CanDash()
    {
        if (Climbing) return false;
        if (!IsAlive || !CanMove || IsDashing)
            return false;
        if (Time.time < lastDashTime + dashCooldown)
            return false;

        return touchingDirections.IsGrounded || canAirDash;
    }


    public void OnHit(int damage, Vector2 knockback)
    {
        if (lockInput) return;
        Climbing = false;
        rb.gravityScale = gravityScale;
        rb.linearVelocity = new Vector2(knockback.x, rb.linearVelocity.y + knockback.y);
    }
    

    public void OnHeal(InputAction.CallbackContext context)
    {
        if (lockInput) return;
        if (Climbing) return;
        if (context.performed)
        {
            stat.Heal();
        }
    }


    public void OnSlide()
    {
        if (lockInput) return;

        if(touchingDirections.IsOnSlidable)
        {
            touchingDirections.sliable.Slide(GetComponent<CapsuleCollider2D>());
        }
    }

    public void OnClimb()
    {
        if(lockInput) return;
        if(touchingDirections.canClimb)
        {
            
            rb.gravityScale = 0;
            OnSlide();
            rb.linearVelocity = new Vector2(0.0f, lookInput.y * walkSpeed);
            
            Climbing = true;
        }
    }

    public void RunForwardForXDistance(float X)
    {
        lockInput = true;
        Debug.Log("On X" + X);
        if(X > 0 )
        {
            rb.linearVelocity = new Vector2(Mathf.Max(Mathf.Abs(rb.linearVelocity.x), walkSpeed), rb.linearVelocityY);
            SetFacingDirection(new Vector2(1, 0));
            lockDirection = new Vector2(1, 0);
        } else
        {
            rb.linearVelocity = new Vector2(-Mathf.Max(Mathf.Abs(rb.linearVelocity.x), walkSpeed), rb.linearVelocityY);
            X = -X;
            SetFacingDirection(new Vector2(-1, 0));
            lockDirection = new Vector2(-1, 0);
        }
        IsMoving = true;

        TargetMoveX = X;
    }

    public void RunForwardForXMilisecondsValue()
    {
        lockInput = true;
    }


    // When fall out of map or fall into traps
    private void Revive() 
    {
        Teleport(SafeGround);
    }

    public void SetSafeGround(Vector3 position)
    {
        SafeGround = position;
        SafeGround.y = LastOnGroundY;
    }

    public void Teleport(Vector3 tele_position, bool onGround = true)
    {
        transform.position = tele_position;
        oldTransformPosition = transform.position;

        if(onGround) {
            int groundLayer = 1 << LayerMask.NameToLayer("Ground");

            RaycastHit2D hit = Physics2D.Raycast(
                tele_position,
                Vector2.down,
                50f,
                groundLayer
            );

            if (hit)
            {
                transform.position = hit.point + Vector2.up * (col.bounds.extents.y + 0.01f);
                oldTransformPosition = transform.position;
            }


        }
    }
}
