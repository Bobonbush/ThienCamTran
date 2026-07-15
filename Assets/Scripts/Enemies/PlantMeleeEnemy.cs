using UnityEngine;

[RequireComponent(typeof(Animator), typeof(Damageable), typeof(Rigidbody2D))]
public class PlantMeleeEnemy : MonoBehaviour
{
    public DetectionZone attackZone;

    [Header("Attack")]
    public int attackDamage = 8;
    public float attackRange = 1.75f;
    public Vector2 knockback = new Vector2(5f, 2f);
    public bool lockFacingDuringAttack;

    [Header("Knockback Resistance")]
    [Tooltip("0 = bị đánh bật như cũ, 1 = trụ hoàn toàn khi trúng đòn.")]
    [Range(0f, 1f)] public float knockbackResistance = 0f;
    [Tooltip("Đang vung đòn thì không bị đẩy lùi, giữ nguyên nhịp đánh.")]
    public bool hyperArmorWhileAttacking = true;

    [Header("Animation Hitbox")]
    [Tooltip("Dame đến từ hitbox keyframe trong clip (kiểu Knight/SwordAttack) thay vì event AttackImpact.")]
    public bool useAnimationHitbox;
    public Collider2D animationAttackHitbox;

    [Header("Line of Sight")]
    [Tooltip("Layer chặn tầm nhìn. Để trống sẽ tự lấy layer Ground.")]
    public LayerMask sightBlockerMask;

    Animator animator;
    Damageable damageable;
    Rigidbody2D rb;
    Collider2D bodyCollider;
    EnemyMove enemyMove;
    EnemySfxController sfx;
    Collider2D currentTarget;
    float attackFacingSign = 1f;

    bool _hasTarget = false;
    public bool HasTarget
    {
        get { return _hasTarget; }
        private set
        {
            _hasTarget = value;
            animator.SetBool(AnimationStrings.hasTarget, value);
        }
    }

    // Animator tự vào state Attack khi hasTarget && attackCooldown < 0.00001 (giống Knight).
    // SetFloatBehaviour trên state Attack reset cooldown khi thoát state.
    public float AttackCooldown
    {
        get { return animator.GetFloat(AnimationStrings.attackCooldown); }
        private set { animator.SetFloat(AnimationStrings.attackCooldown, Mathf.Max(value, 0)); }
    }

    private bool IsAttacking
    {
        get { return animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"); }
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        damageable = GetComponent<Damageable>();
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        enemyMove = GetComponent<EnemyMove>();
        sfx = GetComponent<EnemySfxController>();

        if (sightBlockerMask.value == 0)
            sightBlockerMask = LayerMask.GetMask("Ground");

        damageable.damageableHit.AddListener(OnHit);
        animator.SetBool(AnimationStrings.canMove, true);
        animator.SetBool(AnimationStrings.isAlive, damageable.IsAlive);
        DisableAnimationHitbox();
    }

    private void Update()
    {
        if (!damageable.IsAlive)
        {
            StopAttacking();
            return;
        }

        if (AttackCooldown > 0)
            AttackCooldown -= Time.deltaTime;

        currentTarget = GetTarget();
        HasTarget = currentTarget != null;
        bool hasTarget = HasTarget;
        bool isAttacking = IsAttacking;

        if (enemyMove != null)
        {
            enemyMove.shouldMove = !hasTarget && !isAttacking;
            enemyMove.lockedMove = hasTarget || isAttacking;
        }

        if (isAttacking)
        {
            rb.linearVelocityX = 0f;

            if (lockFacingDuringAttack)
                MaintainAttackFacing();
        }

        if (!hasTarget)
            return;

        if (!isAttacking || !lockFacingDuringAttack)
            FaceTarget(currentTarget.transform.position);

        rb.linearVelocityX = 0f;
    }

    // Animation Event ở frame đầu clip attack: sfx vung + khoá hướng đánh
    public void AttackWindup()
    {
        attackFacingSign = Mathf.Sign(transform.localScale.x);
        if (sfx != null)
            sfx.PlayAttackWindup();
    }

    // Animation Event trong clip attack gọi hàm này (giống Mushroom.FireProjectile)
    public void AttackImpact()
    {
        if (!damageable.IsAlive)
            return;

        if (sfx != null)
            sfx.PlayAttackImpact();

        if (!useAnimationHitbox)
            TryDamageTarget();
    }

    private void TryDamageTarget()
    {
        if (currentTarget == null || currentTarget.GetComponentInParent<PlayerController>() == null)
            return;

        float facingSign = lockFacingDuringAttack
            ? (attackFacingSign >= 0f ? 1f : -1f)
            : (transform.localScale.x >= 0f ? 1f : -1f);
        float targetDirection = currentTarget.bounds.center.x - transform.position.x;
        if (targetDirection * facingSign < 0f)
            return;

        Vector2 targetPoint = currentTarget.bounds.ClosestPoint(transform.position);
        if (Vector2.Distance(transform.position, targetPoint) > attackRange)
            return;

        if (!HasLineOfSight(currentTarget))
            return;

        Damageable targetDamageable = currentTarget.GetComponentInParent<Damageable>();
        if (targetDamageable == null)
            return;

        Vector2 deliveredKnockback = facingSign >= 0f
            ? knockback
            : new Vector2(-knockback.x, knockback.y);

        targetDamageable.Hit(attackDamage, deliveredKnockback, transform.position);
    }

    private Collider2D GetTarget()
    {
        if (attackZone == null)
            return null;

        for (int i = attackZone.detectedColliders.Count - 1; i >= 0; i--)
        {
            Collider2D candidate = attackZone.detectedColliders[i];
            if (candidate == null)
            {
                attackZone.detectedColliders.RemoveAt(i);
                continue;
            }

            if (candidate.GetComponentInParent<PlayerController>() != null && HasLineOfSight(candidate))
                return candidate;
        }

        return null;
    }

    // Không cho nhìn xuyên tường: linecast về phía nhân vật, vướng Ground là mất dấu
    private bool HasLineOfSight(Collider2D target)
    {
        Vector2 origin = bodyCollider != null ? bodyCollider.bounds.center : (Vector2)transform.position;

        if (Physics2D.Linecast(origin, target.bounds.center, sightBlockerMask).collider == null)
            return true;

        // Tâm bị che nhưng mép gần nhất có thể vẫn hở
        Vector2 closestPoint = target.bounds.ClosestPoint(origin);
        return Physics2D.Linecast(origin, closestPoint, sightBlockerMask).collider == null;
    }

    private void FaceTarget(Vector2 targetPosition)
    {
        bool faceRight = targetPosition.x >= transform.position.x;
        float previousSign = Mathf.Sign(transform.localScale.x);

        if (enemyMove != null)
        {
            enemyMove.WalkDirection = faceRight
                ? EnemyMove.WalkableDirection.Right
                : EnemyMove.WalkableDirection.Left;
        }
        else
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (faceRight ? 1f : -1f);
            transform.localScale = scale;
        }

        if (Mathf.Sign(transform.localScale.x) != previousSign)
            Physics2D.SyncTransforms();
    }

    private void MaintainAttackFacing()
    {
        float facingSign = attackFacingSign >= 0f ? 1f : -1f;
        Vector3 scale = transform.localScale;

        if (Mathf.Sign(scale.x) == facingSign)
            return;

        scale.x = Mathf.Abs(scale.x) * facingSign;
        transform.localScale = scale;

        if (enemyMove != null)
        {
            enemyMove.WalkDirection = facingSign > 0f
                ? EnemyMove.WalkableDirection.Right
                : EnemyMove.WalkableDirection.Left;

            scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * facingSign;
            transform.localScale = scale;
        }

        Physics2D.SyncTransforms();
    }

    public void OnHit(int damage, Vector2 hitKnockback)
    {
        float shoveScale = hyperArmorWhileAttacking && IsAttacking
            ? 0f
            : 1f - knockbackResistance;

        if (shoveScale > 0f)
        {
            rb.linearVelocity = new Vector2(
                hitKnockback.x * shoveScale,
                rb.linearVelocity.y + hitKnockback.y * shoveScale);
        }
        else
        {
            // Trụ lại chịu đòn: chặn cả đà trượt ngang đang có
            rb.linearVelocityX = 0f;
        }

        if (enemyMove != null)
        {
            enemyMove.BloodEffect(hitKnockback.normalized);
            enemyMove.Flash();
        }
    }

    private void StopAttacking()
    {
        currentTarget = null;
        DisableAnimationHitbox();

        if (enemyMove != null)
        {
            enemyMove.shouldMove = damageable.IsAlive;
            enemyMove.lockedMove = false;
        }
    }

    private void OnDisable()
    {
        currentTarget = null;
        DisableAnimationHitbox();
    }

    private void DisableAnimationHitbox()
    {
        if (animationAttackHitbox != null)
            animationAttackHitbox.enabled = false;
    }

    private void OnDestroy()
    {
        if (damageable != null)
            damageable.damageableHit.RemoveListener(OnHit);
    }
}
