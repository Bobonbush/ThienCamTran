using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Damageable))]
public class Mushroom : MonoBehaviour
{
    public DetectionZone attackZone;

    [Tooltip("Face to target")]
    public bool faceTarget = true;

    Rigidbody2D rb;
    Animator animator;
    Damageable damageable;
    ProjectileLauncher projectileLauncher;

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
        projectileLauncher = GetComponentInChildren<ProjectileLauncher>();
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

    // Lật mặt về phía mục tiêu gần nhất trong vùng phát hiện
    private void FaceTowardsTarget()
    {
        Collider2D target = attackZone.detectedColliders[0];
        if (target == null) return;

        float dirX = target.transform.position.x - transform.position.x;
        float dirY = target.transform.position.y - transform.position.y;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (dirX >= 0 ? 1 : -1);
        transform.localScale = scale;

        //Vector2 dir = Vector2.Normalize(new Vector2(dirX, dirY));

    }

    // Animation Event trong clip mushroom_attack gọi hàm này
    public void FireProjectile()
    {
        projectileLauncher.FireProjectile();
    }

    public void OnHit(int damage, Vector2 knockback)
    {
        rb.linearVelocity = new Vector2(knockback.x, rb.linearVelocity.y + knockback.y);
    }
}
