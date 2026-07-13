using System.Collections;
using UnityEngine;

public interface IDirectionalDamageBlocker
{
    bool BlocksDamageFrom(Vector2 damageSource);
    void OnDamageBlocked(Vector2 damageSource);
}

[RequireComponent(typeof(Animator), typeof(Damageable), typeof(Rigidbody2D))]
public class RangeEnemy : MonoBehaviour, IDirectionalDamageBlocker
{
    public enum EnemyType
    {
        Knife,
        Bomb,
        Shield,
        Staff
    }

    [Header("Enemy")]
    public EnemyType enemyType = EnemyType.Knife;
    public DetectionZone attackZone;
    public bool faceTarget = true;
    public bool stationary = true;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform launchPoint;
    public float straightProjectileSpeed = 12f;
    public Vector2 bombLaunchVelocity = new Vector2(6f, 8f);

    [Header("Timing")]
    [Min(0f)] public float attackWindup = 0.35f;
    [Min(0f)] public float attackCooldown = 1.75f;
    [Min(0f)] public float shieldDuration = 2.5f;

    [Header("Shield")]
    [Range(0f, 180f)] public float shieldBlockArc = 160f;

    [Header("Hover")]
    public bool hovering;
    [Min(0f)] public float hoverAmplitude = 0.25f;
    [Min(0.01f)] public float hoverFrequency = 1.2f;
    [Min(0.1f)] public float hoverResponsiveness = 8f;

    private static readonly int HasTargetHash = Animator.StringToHash("hasTarget");
    private static readonly int RangedAttackHash = Animator.StringToHash("rangedAttack");
    private static readonly int IsShieldingHash = Animator.StringToHash("isShielding");
    private static readonly int BlockHitHash = Animator.StringToHash("blockHit");
    private static readonly int HasStaffHash = Animator.StringToHash("hasStaff");
    private static readonly int CatchProjectileHash = Animator.StringToHash("catchProjectile");

    private Animator animator;
    private Damageable damageable;
    private Rigidbody2D rb;
    private EnemyMove enemyMove;
    private Coroutine combatRoutine;
    private Collider2D currentTarget;
    private bool isShielding;
    private bool hasStaff = true;
    private float lastFacingSign;
    private float hoverCenterX;
    private float hoverCenterY;
    private float hoverPhase;

    public bool IsShielding
    {
        get { return isShielding; }
    }

    public Vector2 StaffReturnPosition
    {
        get { return LaunchPosition; }
    }

    private Vector2 LaunchPosition
    {
        get { return launchPoint != null ? launchPoint.position : transform.position; }
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        damageable = GetComponent<Damageable>();
        damageable.damageableHit.AddListener(OnHit);

        rb = GetComponent<Rigidbody2D>();
        enemyMove = GetComponent<EnemyMove>();

        animator.SetBool(AnimationStrings.canMove, !stationary && !hovering);
        animator.SetBool(AnimationStrings.isAlive, damageable.IsAlive);
        if (enemyType == EnemyType.Staff)
            animator.SetBool(HasStaffHash, true);
        lastFacingSign = Mathf.Sign(transform.localScale.x);

        if (hovering)
        {
            hoverCenterX = transform.position.x;
            hoverCenterY = transform.position.y;
            hoverPhase = Mathf.Abs(transform.position.x * 0.173f);
            rb.gravityScale = 0f;
            rb.linearVelocityY = 0f;
        }

        if (enemyMove != null && (stationary || hovering))
        {
            enemyMove.stationalEnemy = true;
            enemyMove.shouldMove = false;
        }
    }

    private void FixedUpdate()
    {
        if (!hovering || !damageable.IsAlive)
            return;

        float angle = (Time.fixedTime + hoverPhase) * hoverFrequency * Mathf.PI * 2f;
        float targetY = hoverCenterY + Mathf.Sin(angle) * hoverAmplitude;
        rb.linearVelocityX = (hoverCenterX - rb.position.x) * hoverResponsiveness;
        rb.linearVelocityY = (targetY - rb.position.y) * hoverResponsiveness;
    }

    private void LateUpdate()
    {
        if (enemyMove == null)
            return;

        float facingSign = Mathf.Sign(transform.localScale.x);
        if (facingSign == lastFacingSign)
            return;

        lastFacingSign = facingSign;
        Physics2D.SyncTransforms();
    }

    private void Update()
    {
        if (!damageable.IsAlive)
        {
            if (hovering)
                rb.linearVelocity = Vector2.zero;

            StopCombat();
            return;
        }

        currentTarget = GetTarget();
        bool hasTarget = currentTarget != null;
        animator.SetBool(HasTargetHash, hasTarget);

        if (!hasTarget)
        {
            StopCombat();
            return;
        }

        if (faceTarget)
            FaceTarget(currentTarget.transform.position);

        if (combatRoutine == null && (enemyType != EnemyType.Staff || hasStaff))
            combatRoutine = StartCoroutine(CombatLoop());
    }

    private IEnumerator CombatLoop()
    {
        if (enemyType == EnemyType.Shield)
        {
            SetShielding(true);
            yield return new WaitForSeconds(shieldDuration);
            SetShielding(false);
        }

        animator.SetTrigger(RangedAttackHash);
        yield return new WaitForSeconds(attackWindup);

        if (currentTarget != null && damageable.IsAlive)
            FireProjectile();

        yield return new WaitForSeconds(attackCooldown);
        combatRoutine = null;
    }

    public void FireProjectile()
    {
        if (projectilePrefab == null)
            return;

        Vector2 direction = GetFacingDirection();

        GameObject projectile = Instantiate(projectilePrefab, LaunchPosition, projectilePrefab.transform.rotation);
        Vector3 scale = projectile.transform.localScale;
        float projectileFacing = Mathf.Sign(direction.x);
        if (enemyType == EnemyType.Knife || enemyType == EnemyType.Shield)
            projectileFacing *= -1f;

        projectile.transform.localScale = new Vector3(Mathf.Abs(scale.x) * projectileFacing, scale.y, scale.z);

        if (enemyType == EnemyType.Staff)
        {
            CatStaffProjectile staffProjectile = projectile.GetComponent<CatStaffProjectile>();
            if (staffProjectile != null)
            {
                hasStaff = false;
                animator.SetBool(HasStaffHash, false);
                staffProjectile.Launch(this, direction, straightProjectileSpeed);
                return;
            }
        }

        if (enemyType == EnemyType.Bomb)
        {
            Vector2 velocity = new Vector2(
                Mathf.Abs(bombLaunchVelocity.x) * GetFacingDirection().x,
                bombLaunchVelocity.y);

            ProjectileTrajectory trajectory = projectile.GetComponent<ProjectileTrajectory>();
            if (trajectory != null)
            {
                trajectory.Launch(velocity);
                return;
            }

            Rigidbody2D projectileBody = projectile.GetComponent<Rigidbody2D>();
            if (projectileBody != null)
                projectileBody.linearVelocity = velocity;
            return;
        }

        Projectile straightProjectile = projectile.GetComponent<Projectile>();
        if (straightProjectile != null)
        {
            Rigidbody2D projectileBody = projectile.GetComponent<Rigidbody2D>();
            if (projectileBody != null)
                projectileBody.gravityScale = 0f;

            straightProjectile.Launch(direction * straightProjectileSpeed);
            return;
        }

        Rigidbody2D body = projectile.GetComponent<Rigidbody2D>();
        if (body != null)
            body.linearVelocity = direction * straightProjectileSpeed;
    }

    public bool BlocksDamageFrom(Vector2 damageSource)
    {
        if (enemyType != EnemyType.Shield || !isShielding || !damageable.IsAlive)
            return false;

        Vector2 toSource = damageSource - (Vector2)transform.position;
        if (toSource.sqrMagnitude < 0.001f)
            return true;

        float minimumDot = Mathf.Cos(shieldBlockArc * 0.5f * Mathf.Deg2Rad);
        return Vector2.Dot(GetFacingDirection(), toSource.normalized) >= minimumDot;
    }

    public void OnDamageBlocked(Vector2 damageSource)
    {
        animator.SetTrigger(BlockHitHash);
    }

    public void OnStaffReturned()
    {
        if (enemyType != EnemyType.Staff || hasStaff || !damageable.IsAlive)
            return;

        hasStaff = true;
        animator.SetBool(HasStaffHash, true);
        animator.SetTrigger(CatchProjectileHash);
    }

    public void OnHit(int damage, Vector2 knockback)
    {
        rb.linearVelocity = new Vector2(knockback.x, rb.linearVelocity.y + knockback.y);

        if (enemyMove != null)
        {
            enemyMove.BloodEffect(knockback.normalized);
            enemyMove.Flash();
        }
    }

    private Collider2D GetTarget()
    {
        if (attackZone == null)
            return null;

        for (int i = attackZone.detectedColliders.Count - 1; i >= 0; i--)
        {
            Collider2D candidate = attackZone.detectedColliders[i];
            if (candidate != null && candidate.GetComponentInParent<PlayerController>() != null)
                return candidate;

            attackZone.detectedColliders.RemoveAt(i);
        }

        return null;
    }

    private void FaceTarget(Vector2 targetPosition)
    {
        float direction = targetPosition.x >= transform.position.x ? 1f : -1f;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;
    }

    private Vector2 GetFacingDirection()
    {
        return transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
    }

    private void SetShielding(bool value)
    {
        isShielding = value;
        animator.SetBool(IsShieldingHash, value);
    }

    private void StopCombat()
    {
        currentTarget = null;

        if (animator != null)
        {
            animator.SetBool(HasTargetHash, false);
            SetShielding(false);
        }
        else
        {
            isShielding = false;
        }

        if (combatRoutine == null)
            return;

        StopCoroutine(combatRoutine);
        combatRoutine = null;
    }

    private void OnDisable()
    {
        currentTarget = null;
        isShielding = false;

        if (combatRoutine != null)
        {
            StopCoroutine(combatRoutine);
            combatRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (damageable != null)
            damageable.damageableHit.RemoveListener(OnHit);
    }

}
