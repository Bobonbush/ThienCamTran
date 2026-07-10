using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Damageable))]
[RequireComponent(typeof(Inventory))]
public class PlayerController : MonoBehaviour
{


    [SerializeField]
    private GameObject atk1;

    [SerializeField]
    private GameObject atk2;

    [SerializeField]
    private GameObject atk3;

    [SerializeField]
    private GameObject air_atk;

    [SerializeField]
    private float dashToAttack = 0.1f;

    [SerializeField]
    private float maxComboTime = 0.3f;


   

    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpImpulse = 10f;
    public float airWalkSpeed = 3f;

    public float maxSpeedFall = 50f;
    public float gravityMultiplier = 2.0f;

    public float dashSpeed = 200f;
    public float dashCooldown = 0.8f;
    public float interactRange = 1.5f;
    public LayerMask interactLayerMask = ~0;

    private float lastDashTime = -999f;
    private int dashDir = 1;
    private bool canAirDash = true;
    private bool wasDashing = false;
    private bool isHoldingJump;

    public bool lockInput = false;

    private float TargetMoveX = 0.0f;

    private float maxJumpTime = 1.5f;
    private float Jumptiming = 0.0f;
    
    private Vector3 oldTransformPosition = Vector3.zero;
    private Vector2 lockDirection = new Vector2(1, 0);


    [SerializeField]
    private bool isAttacking = false;

    PlayerStats stat;

    Vector2 moveInput;
    TouchingDirections touchingDirections;
    Damageable damageable;
    Inventory inventory;

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

    [SerializeField]
    private bool _isDucking = false;
    public bool IsDucking
    {
        get
        {
            return _isDucking;
        }

        private set
        {
            _isDucking = value;
            animator.SetBool(AnimationStrings.isDucking, value);
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
    Item promptedItem;
    ItemContainer promptedContainer;

    private bool Climbing = false;

    private bool notFallYet = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        touchingDirections = GetComponent<TouchingDirections>();
        damageable = GetComponent<Damageable>();
        stat = GetComponent<PlayerStats>();
        col = GetComponent<CapsuleCollider2D>();
        gravityScale = rb.gravityScale;
        inventory = GetComponent<Inventory>();
    }

    private void Update()
    {
        if (damageable.ReviveAction)
        {
            damageable.ReviveAction = false;
            Revive();
        }

        if (isAttacking)
        {
            return;
        }
        

        if(touchingDirections.IsGrounded)
        {
            LastOnGroundY = transform.position.y;
            canAirDash = true;
        }

        UpdateInteractPrompts();

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            
            InteractWithNearest();
        }

        if(isHoldingJump)
        {
            Jumptiming += Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {

        if (isAttacking)
        {
            return;
        }
        if (lockInput )
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
            damageable.setInvisibleFrame(0.1f);
        }
        else 
        {
            if (wasDashing)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (!damageable.LockVelocity)
            {
                if (Climbing == false)
                {
                    rb.linearVelocity = new Vector2(moveInput.x * CurrentSpeed, rb.linearVelocity.y);
                    SetFacingDirection(moveInput);
                    
                    
                    if (rb.linearVelocityY < 0.0f && !wasDashing)
                    {
                        notFallYet = false;
                        rb.gravityScale = gravityScale * gravityMultiplier;
                    } else if(notFallYet == false)
                    {
                        rb.gravityScale = gravityScale;
                    }

                    rb.linearVelocity = new Vector2(rb.linearVelocityX, Mathf.Max(rb.linearVelocityY, -maxSpeedFall));
                }
                else
                {

                    rb.linearVelocity = new Vector2(0.0f, lookInput.y * walkSpeed);
                    rb.gravityScale = 0.0f;
                    if (touchingDirections.canClimb == false)
                    {
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
        if(isAttacking)
        {
            return;
        }
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

                IsDucking = true;
            } else
            {
                IsDucking = false;
            }
        } else
        {
            IsDucking = false;
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
           

            rb.gravityScale = gravityScale;

            if (IsDucking && touchingDirections.IsOnSlidable)
            {

                OnSlide();
                return;
            }
        }

        


        if (context.started) {  
            isHoldingJump = true; Jumptiming = 0.0f; rb.gravityScale = gravityScale;
            IsDucking = false;
        }
        if (context.canceled || isAttacking) {
            rb.gravityScale = gravityMultiplier / Mathf.Min(1.0f, Mathf.Max(Jumptiming / maxJumpTime , 0.2f));
            isHoldingJump = false;
            notFallYet = true;
        }

        if (context.started && (touchingDirections.IsGrounded || Climbing) && CanMove)
        {
            Climbing = false;
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
            animator.SetTrigger(AnimationStrings.attackTrigger);
        }
    }

    public void StopAttack()
    {
        isAttacking = false;
        rb.gravityScale = gravityScale;
    }

    public void Attack1Trigger()
    {
        atk1.GetComponent<TriggerAttack>().Trigger();
        isAttacking = true;
        //transform.position = new Vector3(transform.position.x + dashToAttack * (IsFacingRight ? 1 : -1), transform.position.y, transform.position.z);
    }

    public void Attack2Trigger()
    {
        
        atk2.GetComponent<TriggerAttack>().Trigger();
        isAttacking = true;
        //transform.position = new Vector3(transform.position.x + dashToAttack * (IsFacingRight ? 1 : -1), transform.position.y, transform.position.z);
    }

    public void Attack3Trigger()
    {
        atk3.GetComponent<TriggerAttack>().Trigger();
        isAttacking = true;

        //transform.position = new Vector3(transform.position.x + dashToAttack * (IsFacingRight ? 1 : -1) * 2, transform.position.y, transform.position.z);
    }

    public void AttackAirTrigger()
    {
        air_atk.GetComponent<TriggerAttack>().Trigger();
        isAttacking = true;
        rb.gravityScale = 0.0f;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
        {
            InteractWithNearest();
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
        isAttacking = false;
        Climbing = false;
        rb.gravityScale = gravityScale;
        rb.linearVelocity = new Vector2(knockback.x, rb.linearVelocity.y + knockback.y);
    }
    

    public void OnHeal(InputAction.CallbackContext context)
    {
        if (lockInput) return;
        if (Climbing) return;
        if(touchingDirections.IsGrounded)
        {
            if (context.performed)
            {
                animator.SetTrigger(AnimationStrings.useItem);
            }
        }
        
    }


    public void OnSlide()
    {
        if (lockInput) return;

        if(touchingDirections.IsOnSlidable)
        {
            Debug.Log("Slide");
            touchingDirections.sliable.Slide(GetComponent<CapsuleCollider2D>(), GetComponent<BoxCollider2D>());
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

    private void InteractWithNearest()
    {
        IInteractable interactable = FindNearestInteractable();
        if (interactable != null && interactable.CanInteract)
            interactable.Interact(this);
    }

    private void UpdateInteractPrompts()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactRange, interactLayerMask);
        Item nearestItem = null;
        ItemContainer nearestContainer = null;
        float bestDistance = float.MaxValue;

        foreach (Collider2D col in colliders)
        {
            Item item = col.GetComponentInParent<Item>();
            ItemContainer container = col.GetComponentInParent<ItemContainer>();

            if (item != null)
            {
                float distance = Vector2.Distance(transform.position, item.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearestItem = item;
                    nearestContainer = null;
                }
            }

            if (container != null)
            {
                float distance = Vector2.Distance(transform.position, container.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearestItem = null;
                    nearestContainer = container;
                }
            }
        }

        foreach (Collider2D col in colliders)
        {
            Item item = col.GetComponentInParent<Item>();
            if (item != null)
                item.SetPromptVisible(item == nearestItem && item.CanInteract);

            ItemContainer container = col.GetComponentInParent<ItemContainer>();
            if (container != null)
                container.SetPromptVisible(container == nearestContainer && container.CanInteract);
        }

        if (promptedItem != null && promptedItem != nearestItem)
            promptedItem.SetPromptVisible(false);

        if (promptedContainer != null && promptedContainer != nearestContainer)
            promptedContainer.SetPromptVisible(false);

        promptedItem = nearestItem;
        promptedContainer = nearestContainer;
    }

    private IInteractable FindNearestInteractable()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactRange, interactLayerMask);
        IInteractable nearest = null;
        float bestDistance = float.MaxValue;

        foreach (Collider2D col in colliders)
        {
            IInteractable interactable = col.GetComponentInParent<IInteractable>();
            if (interactable == null || !interactable.CanInteract)
                continue;

            float distance = Vector2.Distance(transform.position, col.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = interactable;
            }
        }

        return nearest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }

    public void Teleport(Vector3 tele_position, bool onGround = true)
    {
        transform.position = tele_position;
        oldTransformPosition = transform.position;


        BoxCollider2D col = GetComponent<BoxCollider2D>();
        

        if(onGround) {
            int groundLayer = 1 << LayerMask.NameToLayer("Ground");
            groundLayer |= (1 << LayerMask.NameToLayer("Slidable"));
            RaycastHit2D hit = Physics2D.Raycast(
                tele_position,
                Vector2.down,
                50f,
                groundLayer
            );

            if (hit)
            {
                transform.position = hit.point + Vector2.up * (col.bounds.extents.y * 5.0f);
                oldTransformPosition = transform.position;
            }


        }

        SetSafeGround(transform.position);
    }
}
