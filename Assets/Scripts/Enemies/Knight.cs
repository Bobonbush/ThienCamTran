using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Damageable))]
public class Knight : MonoBehaviour
{
    public float walkAcceleration = 50f;
    public float maxSpeed = 3f;
    public float walkStopRate = 0.1f;
    public DetectionZone attackZone;
    public DetectionZone cliffDetectionZone;
    public bool moveInRange = false;
    public BoxCollider2D moveRange;

    Rigidbody2D rb;
    TouchingDirections touchingDirections;
    Animator animator;
    Damageable damageable;
    Bounds moveRangeBounds;
    bool hasMoveRangeBounds;
    public enum WalkableDirection
    {
        Right,
        Left
    }

    private WalkableDirection _walkDirection;
    private Vector2 walkDiretionVector = Vector2.right;

    public WalkableDirection WalkDirection
    {
        get { return _walkDirection; }
        set
        {
            if (_walkDirection != value)
            {
                gameObject.transform.localScale = new Vector2(gameObject.transform.localScale.x * - 1, gameObject.transform.localScale.y);

                if (value == WalkableDirection.Right)
                {
                    walkDiretionVector = Vector2.right;
                }else if (value == WalkableDirection.Left)
                {
                    walkDiretionVector = Vector2.left;
                }
            }
            _walkDirection = value;
        }
    }
    public bool _hasTarget = false;
    public bool HasTarget { 
        get { 
            return _hasTarget; 
        } 
        private set {
            _hasTarget = value;
            animator.SetBool(AnimationStrings.hasTarget, value);
        } 
    }


    public bool CanMove
    {
        get
        {
            return animator.GetBool(AnimationStrings.canMove);
        }
    }

    public float AttackCooldown { 
        get 
        {
            return animator.GetFloat(AnimationStrings.attackCooldown); 
        } 
        private set 
        { 
            animator.SetFloat(AnimationStrings.attackCooldown, Mathf.Max(value, 0)); 
        } 
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        touchingDirections = GetComponent<TouchingDirections>();
        animator = GetComponent<Animator>();
        damageable = GetComponent<Damageable>();

        if (moveRange != null)
        {
            moveRangeBounds = moveRange.bounds;
            hasMoveRangeBounds = true;
        }
    }

    void Update()
    {
        HasTarget = attackZone.detectedColliders.Count > 0;
        if (AttackCooldown > 0)
        {
            AttackCooldown -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (touchingDirections.IsGrounded && touchingDirections.IsOnWall)
        {
            FlipDirection();
        }

        if (moveInRange && hasMoveRangeBounds && IsNextStepOutsideMoveRange())
        {
            KeepInsideMoveRange();
            FlipDirection();
        }

        if (!damageable.LockVelocity)
        {
            if (CanMove && touchingDirections.IsGrounded)
            {
                // Accelerate towards max speed
                rb.linearVelocity = new Vector2(
                    Mathf.Clamp(rb.linearVelocity.x + (walkAcceleration * walkDiretionVector.x * Time.fixedDeltaTime), -maxSpeed, maxSpeed), 
                    rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = new Vector2(Mathf.Lerp(rb.linearVelocityX, 0, walkStopRate), rb.linearVelocity.y);
            }
        }
    }

    private void FlipDirection()
    {
        if (WalkDirection == WalkableDirection.Right)
        {
            WalkDirection = WalkableDirection.Left;
        }
        else if (WalkDirection == WalkableDirection.Left)
        {
            WalkDirection = WalkableDirection.Right;
        } else
        {
            Debug.LogError("Invalid WalkDirection value: " + WalkDirection);
        }
    }

    public void OnHit(int damage, Vector2 knockback)
    {
        rb.linearVelocity = new Vector2(knockback.x, rb.linearVelocity.y + knockback.y);
    }

    public void OnCliffDetected()
    {
        if (touchingDirections.IsGrounded)
        {
            FlipDirection();
        }
    }

    private bool IsNextStepOutsideMoveRange()
    {
        float currentX = transform.position.x;
        float nextX = currentX + walkDiretionVector.x * Mathf.Max(maxSpeed, Mathf.Abs(rb.linearVelocityX)) * Time.fixedDeltaTime;
        return currentX < moveRangeBounds.min.x || currentX > moveRangeBounds.max.x || nextX < moveRangeBounds.min.x || nextX > moveRangeBounds.max.x;
    }

    private void KeepInsideMoveRange()
    {
        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, moveRangeBounds.min.x, moveRangeBounds.max.x);
        transform.position = position;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (!moveInRange)
            return;

        Gizmos.color = Color.green;

        if (hasMoveRangeBounds)
            Gizmos.DrawWireCube(moveRangeBounds.center, moveRangeBounds.size);
        else if (moveRange != null)
            Gizmos.DrawWireCube(moveRange.bounds.center, moveRange.bounds.size);
    }
}
