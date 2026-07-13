using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(Damageable), typeof(Rigidbody2D))]
public class PlantMeleeEnemy : MonoBehaviour
{
    public DetectionZone attackZone;

    [Header("Attack")]
    public int attackDamage = 8;
    public float attackWindup = 0.2f;
    public float attackRecovery = 0.45f;
    public float attackRange = 1.75f;
    public Vector2 knockback = new Vector2(5f, 2f);
    public bool lockFacingDuringAttack;

    [Header("Animation Hitbox")]
    public bool useAnimationHitbox;
    public Collider2D animationAttackHitbox;

    private static readonly int HasTargetHash = Animator.StringToHash("hasTarget");
    private static readonly int MeleeAttackHash = Animator.StringToHash("meleeAttack");
    private static readonly int IsAliveHash = Animator.StringToHash("isAlive");

    private Animator animator;
    private Damageable damageable;
    private Rigidbody2D rb;
    private EnemyMove enemyMove;
    private EnemySfxController sfx;
    private Collider2D currentTarget;
    private Coroutine attackRoutine;
    private float attackFacingSign = 1f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        damageable = GetComponent<Damageable>();
        rb = GetComponent<Rigidbody2D>();
        enemyMove = GetComponent<EnemyMove>();
        sfx = GetComponent<EnemySfxController>();

        damageable.damageableHit.AddListener(OnHit);
        animator.SetBool(AnimationStrings.canMove, true);
        animator.SetBool(IsAliveHash, damageable.IsAlive);
        DisableAnimationHitbox();
    }

    private void Update()
    {
        if (!damageable.IsAlive)
        {
            StopAttacking();
            return;
        }

        currentTarget = GetTarget();
        bool hasTarget = currentTarget != null;
        animator.SetBool(HasTargetHash, hasTarget);

        if (enemyMove != null)
        {
            enemyMove.shouldMove = !hasTarget && attackRoutine == null;
            enemyMove.lockedMove = hasTarget || attackRoutine != null;
        }

        if (attackRoutine != null)
        {
            rb.linearVelocityX = 0f;

            if (lockFacingDuringAttack)
                MaintainAttackFacing();
        }

        if (!hasTarget)
            return;

        if (attackRoutine == null)
        {
            FaceTarget(currentTarget.transform.position);
            attackFacingSign = Mathf.Sign(transform.localScale.x);
            attackRoutine = StartCoroutine(AttackLoop());
        }
        else if (!lockFacingDuringAttack)
        {
            FaceTarget(currentTarget.transform.position);
        }

        rb.linearVelocityX = 0f;
    }

    private IEnumerator AttackLoop()
    {
        animator.SetTrigger(MeleeAttackHash);
        if (sfx != null)
            sfx.PlayAttackWindup();

        yield return new WaitForSeconds(attackWindup);

        if (sfx != null)
            sfx.PlayAttackImpact();

        if (!useAnimationHitbox)
            TryDamageTarget();

        yield return new WaitForSeconds(attackRecovery);
        attackRoutine = null;
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

            if (candidate.GetComponentInParent<PlayerController>() != null)
                return candidate;
        }

        return null;
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
        rb.linearVelocity = new Vector2(hitKnockback.x, rb.linearVelocity.y + hitKnockback.y);

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

        if (attackRoutine == null)
            return;

        StopCoroutine(attackRoutine);
        attackRoutine = null;
    }

    private void OnDisable()
    {
        currentTarget = null;
        attackRoutine = null;
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
