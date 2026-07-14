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
    [Tooltip("Chỉ dùng cho loại Shield (nghỉ sau khi ném). Loại khác chỉnh cooldown bằng SetFloatBehaviour trên state Throw trong Animator.")]
    [Min(0f)] public float attackCooldown = 1.75f;
    [Min(0f)] public float shieldDuration = 2.5f;

    [Header("Shield")]
    [Range(0f, 180f)] public float shieldBlockArc = 160f;

    [Header("Line of Sight")]
    [Tooltip("Bật lên thì nhìn xuyên tường như cũ, tắt thì phải thấy nhân vật mới bắn.")]
    public bool seeThroughWalls = false;
    [Tooltip("Layer chặn tầm nhìn. Để trống sẽ tự lấy layer Ground.")]
    public LayerMask sightBlockerMask;
    [Tooltip("Đạn bay xuyên tường hay chạm tường thì nổ/ghim.")]
    public bool projectilesPierceWalls = false;

    [Header("Bomb Arc")]
    [Tooltip("Ném cầu vồng nhắm đúng vị trí nhân vật thay vì lực cố định.")]
    public bool aimBombAtTarget = true;
    [Tooltip("Góc ném (độ). Quỹ đạo được giải để rơi trúng mục tiêu với đúng góc này.")]
    [Range(15f, 85f)] public float throwAngle = 55f;
    [Min(1f)] public float maxThrowSpeed = 16f;

    [Header("Hover")]
    public bool hovering;
    [Min(0f)] public float hoverAmplitude = 0.25f;
    [Min(0.01f)] public float hoverFrequency = 1.2f;
    [Min(0.1f)] public float hoverResponsiveness = 8f;

    Animator animator;
    Damageable damageable;
    Rigidbody2D rb;
    Collider2D bodyCollider;
    EnemyMove enemyMove;
    EnemySfxController sfx;
    Coroutine combatRoutine;
    Collider2D currentTarget;
    bool isShielding;
    bool hasStaff = true;
    float lastFacingSign;
    float hoverCenterX;
    float hoverCenterY;
    float hoverPhase;

    public bool IsShielding
    {
        get { return isShielding; }
    }

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

    // Animator tự vào state Throw khi hasTarget && attackCooldown < 0.00001 (giống Knight/Mushroom).
    // SetFloatBehaviour trên state Throw reset cooldown khi thoát state. Riêng Shield vẫn chạy theo CombatLoop.
    public float AttackCooldown
    {
        get { return animator.GetFloat(AnimationStrings.attackCooldown); }
        private set { animator.SetFloat(AnimationStrings.attackCooldown, Mathf.Max(value, 0)); }
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
        bodyCollider = GetComponent<Collider2D>();
        enemyMove = GetComponent<EnemyMove>();
        sfx = GetComponent<EnemySfxController>();

        if (sightBlockerMask.value == 0)
            sightBlockerMask = LayerMask.GetMask("Ground");

        animator.SetBool(AnimationStrings.canMove, !stationary && !hovering);
        animator.SetBool(AnimationStrings.isAlive, damageable.IsAlive);
        if (enemyType == EnemyType.Staff)
            animator.SetBool(AnimationStrings.hasStaff, true);
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

        if (enemyType != EnemyType.Shield && AttackCooldown > 0)
            AttackCooldown -= Time.deltaTime;

        currentTarget = GetTarget();
        HasTarget = currentTarget != null;

        if (!HasTarget)
        {
            StopCombat();
            return;
        }

        if (faceTarget)
            FaceTarget(currentTarget.transform.position);

        // Knife/Bomb/Staff: animator tự tấn công qua điều kiện hasTarget + attackCooldown (+ hasStaff).
        // Shield cần trình tự giơ khiên -> ném nên vẫn chạy coroutine.
        if (enemyType == EnemyType.Shield && combatRoutine == null)
            combatRoutine = StartCoroutine(CombatLoop());
    }

    private IEnumerator CombatLoop()
    {
        SetShielding(true);
        if (sfx != null)
            sfx.PlayShieldRaise();
        yield return new WaitForSeconds(shieldDuration);
        SetShielding(false);

        animator.SetTrigger(AnimationStrings.rangedAttackTrigger);

        // Đạn rời tay do Animation Event FireProjectile trong clip Throw quyết định
        yield return new WaitForSeconds(attackCooldown);
        combatRoutine = null;
    }

    // Animation Event ở frame đầu clip Throw
    public void AttackWindup()
    {
        if (sfx != null)
            sfx.PlayAttackWindup();
    }

    // Animation Event trong clip Throw gọi hàm này (giống Mushroom.FireProjectile)
    public void FireProjectile()
    {
        if (projectilePrefab == null || !damageable.IsAlive)
            return;

        Vector2 direction = GetFacingDirection();

        GameObject projectile = Instantiate(projectilePrefab, LaunchPosition, projectilePrefab.transform.rotation);
        if (sfx != null)
            sfx.PlayAttackRelease();

        ApplyWallPiercing(projectile);

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
                animator.SetBool(AnimationStrings.hasStaff, false);
                staffProjectile.Launch(this, direction, straightProjectileSpeed);
                return;
            }
        }

        if (enemyType == EnemyType.Bomb)
        {
            Vector2 velocity = GetBombVelocity(projectile);

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

    private void ApplyWallPiercing(GameObject projectile)
    {
        if (!projectilesPierceWalls)
            return;

        Projectile straightProjectile = projectile.GetComponent<Projectile>();
        if (straightProjectile != null)
            straightProjectile.piercesWalls = true;

        ProjectileTrajectory trajectory = projectile.GetComponent<ProjectileTrajectory>();
        if (trajectory != null)
            trajectory.piercesWalls = true;
    }

    // Giải quỹ đạo ném cầu vồng: cố định góc ném, tính tốc độ để rơi trúng mục tiêu
    private Vector2 GetBombVelocity(GameObject projectile)
    {
        Vector2 fallback = new Vector2(
            Mathf.Abs(bombLaunchVelocity.x) * GetFacingDirection().x,
            bombLaunchVelocity.y);

        if (!aimBombAtTarget || currentTarget == null)
            return fallback;

        Vector2 origin = LaunchPosition;
        Vector2 targetPosition = currentTarget.bounds.center;

        float gravityScale = 1f;
        Rigidbody2D projectileBody = projectile.GetComponent<Rigidbody2D>();
        if (projectileBody != null)
            gravityScale = projectileBody.gravityScale;

        float gravity = Mathf.Abs(Physics2D.gravity.y) * gravityScale;
        float dx = Mathf.Abs(targetPosition.x - origin.x);
        float dy = targetPosition.y - origin.y;

        if (dx < 0.05f || gravity < 0.01f)
            return fallback;

        float angle = throwAngle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angle);
        float denominator = 2f * cos * cos * (dx * Mathf.Tan(angle) - dy);

        // Mục tiêu cao hơn góc ném với tới được
        if (denominator <= 0.001f)
            return fallback;

        float speed = Mathf.Sqrt(gravity * dx * dx / denominator);
        if (speed > maxThrowSpeed)
            return fallback;

        float directionSign = Mathf.Sign(targetPosition.x - origin.x);
        return new Vector2(cos * speed * directionSign, Mathf.Sin(angle) * speed);
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
        animator.SetTrigger(AnimationStrings.blockHitTrigger);
        if (sfx != null)
            sfx.PlayBlock();
    }

    public void OnStaffReturned()
    {
        if (enemyType != EnemyType.Staff || hasStaff || !damageable.IsAlive)
            return;

        hasStaff = true;
        animator.SetBool(AnimationStrings.hasStaff, true);
        animator.SetTrigger(AnimationStrings.catchProjectileTrigger);
        if (sfx != null)
            sfx.PlayCatch();
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
            if (candidate == null)
            {
                attackZone.detectedColliders.RemoveAt(i);
                continue;
            }

            if (candidate.GetComponentInParent<PlayerController>() == null)
                continue;

            if (seeThroughWalls || HasLineOfSight(candidate))
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
        animator.SetBool(AnimationStrings.isShielding, value);
    }

    private void StopCombat()
    {
        currentTarget = null;

        if (animator != null)
        {
            animator.SetBool(AnimationStrings.hasTarget, false);
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
