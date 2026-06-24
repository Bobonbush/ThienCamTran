using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Damageable))]
public class Mushroom : MonoBehaviour
{
    public float walkAcceleration = 35f;
    public float maxSpeed = 2.5f;
    public float walkStopRate = 0.1f;
    public bool moveWhileTargetInRange = false;
    public bool moveInRange = false;
    public BoxCollider2D moveRange;
    public DetectionZone attackZone;
    public DetectionZone cliffDetectionZone;

    [Tooltip("Face to target")]
    public bool faceTarget = true;
    public float horizontalAimHeightTolerance = 0.5f;
    public Vector2 targetHeadOffset = new Vector2(0f, 1f);

    Rigidbody2D rb;
    Animator animator;
    Damageable damageable;
    ProjectileLauncher projectileLauncher;
    TouchingDirections touchingDirections;
    Bounds moveRangeBounds;
    bool hasMoveRangeBounds;
    public enum WalkableDirection
    {
        Right,
        Left
    }

    private WalkableDirection _walkDirection;
    private Vector2 walkDirectionVector = Vector2.right;

    public WalkableDirection WalkDirection
    {
        get { return _walkDirection; }
        set
        {
            if (_walkDirection != value)
            {
                Vector3 scale = transform.localScale;
                scale.x *= -1;
                transform.localScale = scale;

                walkDirectionVector = value == WalkableDirection.Right ? Vector2.right : Vector2.left;
            }

            _walkDirection = value;
        }
    }

    public bool _hasTarget = false;
    public bool HasTarget
    {
        get { return _hasTarget; }
        private set
        {
            _hasTarget = value;
            animator.SetBool(AnimationStrings.hasTarget, value);
        }
    }

    public bool CanMove
    {
        get { return animator.GetBool(AnimationStrings.canMove); }
    }

    public float AttackCooldown
    {
        get { return animator.GetFloat(AnimationStrings.attackCooldown); }
        private set { animator.SetFloat(AnimationStrings.attackCooldown, Mathf.Max(value, 0)); }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        damageable = GetComponent<Damageable>();
        touchingDirections = GetComponent<TouchingDirections>();
        projectileLauncher = GetComponentInChildren<ProjectileLauncher>();

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

        if (faceTarget && HasTarget)
        {
            FaceTowardsTarget();
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

        if (damageable.LockVelocity)
            return;

        bool shouldMove = CanMove && touchingDirections.IsGrounded && (!HasTarget || moveWhileTargetInRange);
        if (shouldMove)
        {
            rb.linearVelocity = new Vector2(
                Mathf.Clamp(rb.linearVelocity.x + (walkAcceleration * walkDirectionVector.x * Time.fixedDeltaTime), -maxSpeed, maxSpeed),
                rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(Mathf.Lerp(rb.linearVelocityX, 0, walkStopRate), rb.linearVelocity.y);
        }
    }

    // Lật mặt về phía mục tiêu gần nhất trong vùng phát hiện
    private void FaceTowardsTarget()
    {
        Collider2D target = attackZone.detectedColliders[0];
        if (target == null) return;

        float dirX = target.transform.position.x - transform.position.x;
        WalkDirection = dirX >= 0 ? WalkableDirection.Right : WalkableDirection.Left;
    }

    // Animation Event trong clip mushroom_attack gọi hàm này
    public void FireProjectile()
    {
        if (projectileLauncher == null)
            return;

        Collider2D target = GetTarget();
        if (target == null)
        {
            projectileLauncher.FireProjectileForward();
            return;
        }

        Vector2 aimDirection = GetAimDirection(target);
        projectileLauncher.FireProjectileInDirection(aimDirection);
    }

    public void OnHit(int damage, Vector2 knockback)
    {
        rb.linearVelocity = new Vector2(knockback.x, rb.linearVelocity.y + knockback.y);
    }

    public void OnCliffDetected()
    {
        if (touchingDirections.IsGrounded)
            FlipDirection();
    }

    private Collider2D GetTarget()
    {
        if (attackZone == null || attackZone.detectedColliders.Count == 0)
            return null;

        return attackZone.detectedColliders[0];
    }

    private Vector2 GetAimDirection(Collider2D target)
    {
        Vector2 origin = projectileLauncher.LaunchPosition;
        Vector2 headPosition = (Vector2)target.transform.position + targetHeadOffset;
        float deltaY = Mathf.Abs(headPosition.y - origin.y);

        if (deltaY <= horizontalAimHeightTolerance)
        {
            float dirX = target.transform.position.x >= transform.position.x ? 1f : -1f;
            return new Vector2(dirX, 0f);
        }

        return (headPosition - origin).normalized;
    }

    private void FlipDirection()
    {
        WalkDirection = WalkDirection == WalkableDirection.Right ? WalkableDirection.Left : WalkableDirection.Right;
    }

    private bool IsNextStepOutsideMoveRange()
    {
        float currentX = transform.position.x;
        float nextX = currentX + walkDirectionVector.x * Mathf.Max(maxSpeed, Mathf.Abs(rb.linearVelocityX)) * Time.fixedDeltaTime;
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
        if (moveInRange)
        {
            Gizmos.color = Color.green;

            if (hasMoveRangeBounds)
                Gizmos.DrawWireCube(moveRangeBounds.center, moveRangeBounds.size);
            else if (moveRange != null)
                Gizmos.DrawWireCube(moveRange.bounds.center, moveRange.bounds.size);
        }
    }
}
