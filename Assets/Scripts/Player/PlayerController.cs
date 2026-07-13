using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Damageable))]
[RequireComponent(typeof(Inventory))]
public class PlayerController : MonoBehaviour
{


    [SerializeField]
    private GeneratePurifyPuzzle puzzleManager;

    [SerializeField]
    private GameObject atk1;

    [SerializeField]
    private GameObject atk2;

    [SerializeField]
    private GameObject atk3;

    [SerializeField]
    private GameObject air_atk;

    private SealedPuzzle inActivePuzzle;

    private bool saveLock = false;

    [SerializeField]
    private float dashToAttack = 0.1f;

    [SerializeField]
    private float maxComboTime = 0.3f;


    private bool StopCombo = true;

    private float lockInputFor = -1.0f;

   

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
    private bool setLock = false;

    private float TargetMoveX = 0.0f;

    private float maxJumpTime = 1.5f;
    private float Jumptiming = 0.0f;
    
    private Vector3 oldTransformPosition = Vector3.zero;
    private Vector2 lockDirection = new Vector2(1, 0);

    private bool SwapSaveDirection = false;
    private bool CanExitForcementState = false;


    [SerializeField]
    private bool isAttacking = false;

    PlayerStats stat;

    Vector2 moveInput;
    TouchingDirections touchingDirections;
    Damageable damageable;
    Inventory inventory;
    InventoryUI inventoryUI;

    Vector3 SafeGround = Vector3.zero;
    float LastOnGroundY = 0;
    float gravityScale = 0.0f;

    private bool isSaving = false;

    private bool isPuzzleSolving = false;

    public bool CutSceneLock = false;


    

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
    InteractiveRoom promptedRoom;
    SealedPuzzle promptedSealedPuzzle;

    private bool Climbing = false;

    private bool notFallYet = false;

    private void Awake()
    {
        if (puzzleManager != null)
        {
            puzzleManager.gameObject.SetActive(false);
            puzzleManager.OnPuzzleCompleted += OnPuzzleComplete;
            puzzleManager.OnPuzzleFail += OnPuzzleFail;
        }

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        touchingDirections = GetComponent<TouchingDirections>();
        damageable = GetComponent<Damageable>();
        stat = GetComponent<PlayerStats>();
        col = GetComponent<CapsuleCollider2D>();
        gravityScale = rb.gravityScale;
        inventory = GetComponent<Inventory>();
        inventoryUI = GetComponent<InventoryUI>();

        if (inventoryUI == null)
            inventoryUI = gameObject.AddComponent<InventoryUI>();
    }

    private void Update()
    {
        if(lockInputFor > 0.0f && setLock)
        {
            lockInput = true;
            lockInputFor -= Time.deltaTime;
        }else if(setLock)
        {
            setLock = false;
        }

        if(isSaving)
        {
            GotoSaving();
        }

        if(isPuzzleSolving)
        {
            GotoPuzzle();
        }

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
        if (CutSceneLock)
        {
            lockInput = true;
        }
        if (lockInput && setLock == false && saveLock == false && CutSceneLock == false)
        {

            TargetMoveX -= Mathf.Abs(transform.position.x - oldTransformPosition.x);
            oldTransformPosition = transform.position;
            if (TargetMoveX <= 0)
            {
                transform.position = new Vector3(transform.position.x - lockDirection.x * TargetMoveX, transform.position.y, transform.position.z);
                IsMoving = false;
                IsRunning = false;
                lockInput = false;
                return;
            }

            rb.linearVelocity = new Vector2(Mathf.Max(CurrentSpeed, rb.linearVelocityX, walkSpeed), rb.linearVelocity.y) * lockDirection;
            
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

    private void LockInput(float timing)
    {
        lockInputFor = timing;
        setLock = true;
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

    // Can be use for open menu

    public void OnCancelForcingState(InputAction.CallbackContext context)
    {
        if(CanExitForcementState)
        {
            if(isSaving)
            {
                ExitSaving();
            }
            if(isPuzzleSolving)
            {
                ExitPuzzle();
            }
        }else
        {
            // Open Settings
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
                IsDucking = false;
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

    public void OnPurify(InputAction.CallbackContext context)
    {
        if (puzzleManager == null)
        {
            return;
        }

        if(!isPuzzleSolving)
        {
            return;
        }
        if(context.performed)
             puzzleManager.Purify(context.ReadValue<Vector2>());

    }

    private void OnPuzzleComplete()
    {
        if (inActivePuzzle == null)
        {
            ExitPuzzle();
            return;
        }

        inActivePuzzle.Done(this, GetComponentInChildren<PlayerCamera>());
        ExitPuzzle();
    }

    private void OnPuzzleFail()
    {
        ExitPuzzle();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (lockInput) return;
        if (Climbing) return;
        if (context.performed)
        {
            if (StopCombo)
            {
                animator.SetTrigger(AnimationStrings.attackTrigger);
                StopCombo = false;
            }
        }
    }

    public void StopAttack()
    {
        isAttacking = false;
        rb.gravityScale = gravityScale;
    }


    public void ComboFrame()
    {
        StopCombo = true;
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
        //if (lockInput) return;

        ExitPuzzle();
        AnimationExitPuzzle();

        isAttacking = false;
        Climbing = false;
        StopCombo = true;
        
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
        {
            if (interactable.GetType() == IInteractable.Type.Item && IsDucking == false)
            {
                LockInput(0.25f);
                
                animator.SetTrigger(AnimationStrings.pickItem);
            }
            interactable.Interact(this);    
        }
    }

    private void InteractWithNearest(Item item)
    {
        if(item != null && item.CanInteract)
        {
            item.Interact(this);
        }
    }

    private void UpdateInteractPrompts()
    {
        if(!touchingDirections.IsGrounded)
        {
            return;
        }
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactRange, interactLayerMask);
        Item nearestItem = null;
        ItemContainer nearestContainer = null;
        InteractiveRoom nearestRoom = null;
        SealedPuzzle nearestSealedPuzzle = null;
        float bestDistance = float.MaxValue;

        foreach (Collider2D col in colliders)
        {
            Item item = col.GetComponentInParent<Item>();
            ItemContainer container = col.GetComponentInParent<ItemContainer>();
            InteractiveRoom room = col.GetComponentInParent<InteractiveRoom>();
            SealedPuzzle sealedPuzzle = col.GetComponentInParent<SealedPuzzle>();

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

            if(room != null)
            {
                float distance = Vector2.Distance(transform.position, room.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearestRoom = room;
                }
            }

            if (sealedPuzzle != null)
            {
                float distance = Vector2.Distance(transform.position, sealedPuzzle.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearestItem = null;
                    nearestSealedPuzzle = sealedPuzzle;
                }
            }
        }

        if (!isPuzzleSolving && !isSaving)
        {
            foreach (Collider2D col in colliders)
            {
                Item item = col.GetComponentInParent<Item>();
                Transform itemTrasnform = col.gameObject.GetComponent<Transform>();

                if (item != null && item.GetType() == IInteractable.Type.Non_needPickup && Mathf.Abs(itemTrasnform.position.y - transform.position.y) <= 0.85f && Mathf.Abs(itemTrasnform.position.x - transform.position.x) <= 0.65f)
                {
                    InteractWithNearest(nearestItem);
                }

                if (item != null)
                    item.SetPromptVisible(item == nearestItem && item.CanInteract);

                ItemContainer container = col.GetComponentInParent<ItemContainer>();
                if (container != null)
                    container.SetPromptVisible(container == nearestContainer && container.CanInteract);

                InteractiveRoom room = col.GetComponentInParent<InteractiveRoom>();
                if (room != null)
                    room.SetPromptVisible(room == nearestRoom && room.CanInteract);

                SealedPuzzle sealedPuzzle = col.GetComponentInParent<SealedPuzzle>();
                if (sealedPuzzle != null)
                {
                    sealedPuzzle.SetPromptVisible(sealedPuzzle == nearestSealedPuzzle && sealedPuzzle.CanInteract);
                }
            }
        }


       
        if (promptedItem != null && promptedItem != nearestItem && nearestItem != null &&  nearestItem.GetType() == IInteractable.Type.Item)
            promptedItem.SetPromptVisible(false);

        if (promptedContainer != null && promptedContainer != nearestContainer)
            promptedContainer.SetPromptVisible(false);
        if(promptedRoom != null && promptedRoom != nearestRoom)
            promptedRoom.SetPromptVisible(false);
        if (promptedSealedPuzzle != null && promptedSealedPuzzle != nearestSealedPuzzle)
            promptedSealedPuzzle.SetPromptVisible(false);


        promptedItem = nearestItem;
        promptedContainer = nearestContainer;
        promptedRoom = nearestRoom;
        promptedSealedPuzzle = nearestSealedPuzzle;
    }

    private IInteractable FindNearestInteractable()
    {
      
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactRange, interactLayerMask);
        IInteractable nearest = null;
        if(!touchingDirections.IsGrounded)
        {
            return nearest;
        }

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


    // Save Game

    public bool EnterSaving(Vector3 left, Vector3 right)
    {

        CanExitForcementState = false;
        isSaving = true;
        float distanceLeft = Mathf.Abs(left.x - transform.position.x);
        float distanceRight = Mathf.Abs(right.x - transform.position.x);

        float offsetLeft = left.x - transform.position.x;
        float offsetRight = right.x - transform.position.x;

        if (distanceLeft < distanceRight)
        {

            if (offsetLeft >= 0.0f)
            {
                SwapSaveDirection = false;
            }else
            {
                SwapSaveDirection = true;
            }
            RunForwardForXDistance(offsetLeft);
        }else
        {
            if(offsetRight < 0)
            {
                SwapSaveDirection = false;
            } else
            {
                SwapSaveDirection = true;
            }
             RunForwardForXDistance(offsetRight);
        }

        return true;
    }

    // Go to the saving position and perform the animation
    public void GotoSaving()
    {
        
        if(!lockInput == false && saveLock == false)
        {
            return;
        }

        

        lockInput = true;
        if (saveLock == false)
        {
            if (SwapSaveDirection)
            {
                if (lockDirection.x < 0)
                {
                    SetFacingDirection(new Vector2(1, 0));
                }
                else
                {
                    SetFacingDirection(new Vector2(-1, 0));
                }
                SwapSaveDirection = false;
            }
            rb.linearVelocity = Vector3.zero;
            saveLock = true;
            animator.SetBool(AnimationStrings.isSaving, true);
            animator.SetTrigger(AnimationStrings.isSaving + " 0");
        }
    }
    
    public bool ExitSaving()
    {
        if(!CanExitForcementState)
        {
            return false;
        }


        isSaving = false;
        animator.SetBool(AnimationStrings.isSaving, false);
        return true;
    }

    public void AnimationExitSaving()
    {
        lockInput = false;
        saveLock = false;

        // Turn off the UI here

    }

    public void AnimationEnableSave()
    {
        CanExitForcementState = true;
        // Implement the UI here.
    }

    public void EnterPuzzle(Vector3 left, Vector3 right, SealedPuzzle puzzle)
    {
        inActivePuzzle = puzzle;

        CanExitForcementState = false;
        isPuzzleSolving = true;
        float distanceLeft = Mathf.Abs(left.x - transform.position.x);
        float distanceRight = Mathf.Abs(right.x - transform.position.x);

        float offsetLeft = left.x - transform.position.x;
        float offsetRight = right.x - transform.position.x;

        if (distanceLeft < distanceRight)
        {

            if (offsetLeft >= 0.0f)
            {
                SwapSaveDirection = false;
            }
            else
            {
                SwapSaveDirection = true;
            }
            RunForwardForXDistance(offsetLeft);
        }
        else
        {
            if (offsetRight < 0)
            {
                SwapSaveDirection = false;
            }
            else
            {
                SwapSaveDirection = true;
            }
            RunForwardForXDistance(offsetRight);
        }
    }

    public void GotoPuzzle()
    {

        if (!lockInput == false && saveLock == false)
        {
            return;
        }



        lockInput = true;
        if (saveLock == false)
        {
            if (SwapSaveDirection)
            {
                if (lockDirection.x < 0)
                {
                    SetFacingDirection(new Vector2(1, 0));
                }
                else
                {
                    SetFacingDirection(new Vector2(-1, 0));
                }
                SwapSaveDirection = false;
            }
            rb.linearVelocity = Vector3.zero;
            saveLock = true;
            animator.SetBool(AnimationStrings.isPuzzling, true);
            animator.SetTrigger(AnimationStrings.isPuzzling + " 0");
        }

    }

    public bool ExitPuzzle()
    {
        isPuzzleSolving = false;
        animator.SetBool(AnimationStrings.isPuzzling, false);
        if (puzzleManager != null)
        {
            puzzleManager.gameObject.SetActive(false);
        }
        return true;
    }

    public void AnimationExitPuzzle()
    {
        lockInput = false;
        saveLock = false;

        // Turn off the UI here

    }

    public void AnimationEnablePuzzle()
    {
        if (puzzleManager == null || inActivePuzzle == null)
        {
            ExitPuzzle();
            return;
        }

        CanExitForcementState = true;
        // Implement the UI here.
        puzzleManager.gameObject.SetActive(true);

        puzzleManager.Generate(inActivePuzzle.Round, inActivePuzzle.Duration);

    }


    public bool isLockingInput()
    {
        return lockInput;
    }

    public bool isPuzzling()
    {
        return isPuzzleSolving;
    }

    public IEnumerator Wait(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    public void LockCutScene()
    {
        CutSceneLock = true;
        lockInput = true;

        Debug.Log("Locked");
        
    }

    public void ReleaseLockCutScene()
    {
        CutSceneLock = false;
        lockInput = false;


    }




}
