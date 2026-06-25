using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Damageable))]
public class Mushroom : MonoBehaviour
{

    public DetectionZone attackZone;

    [Tooltip("Face to target")]
    public bool faceTarget = true;
    public float horizontalAimHeightTolerance = 0.5f;
    public Vector2 targetHeadOffset = new Vector2(0f, 1f);

    public bool moveWhileTargetInRange = false;

    Rigidbody2D rb;
    Animator animator;
    Damageable damageable;
    ProjectileLauncher projectileLauncher;
    TouchingDirections touchingDirections;





    EnemyMove e_move;



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

        e_move = GetComponent<EnemyMove>();
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
        if (damageable.LockVelocity)
            return;
        e_move.shouldMove = e_move.CanMove && touchingDirections.IsGrounded && (!HasTarget || moveWhileTargetInRange);
       
    }

    // Lật mặt về phía mục tiêu gần nhất trong vùng phát hiện
    private void FaceTowardsTarget()
    {
        Collider2D target = attackZone.detectedColliders[0];
        if (target == null) return;

        float dirX = target.transform.position.x - transform.position.x;
        e_move.WalkDirection = dirX >= 0 ? EnemyMove.WalkableDirection.Right : EnemyMove.WalkableDirection.Left;
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
}
