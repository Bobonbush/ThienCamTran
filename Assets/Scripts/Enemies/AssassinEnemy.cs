using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(Damageable), typeof(Rigidbody2D))]
public class AssassinEnemy : MonoBehaviour
{
    [Header("Zones")]
    public DetectionZone attackZone;      // tầm chém thường
    public DetectionZone teleportZone;    // tầm phát hiện để tàng hình áp sát

    [Header("Teleport")]
    [Tooltip("Nghỉ giữa 2 lần tàng hình")]
    [Min(0f)] public float teleportCooldown = 6f;
    [Tooltip("Khớp độ dài clip Assassin_Vanish")]
    [Min(0f)] public float vanishAnimTime = 0.25f;
    [Tooltip("Thời gian biến mất hẳn trước khi bóng báo hiệu xuất hiện ở điểm đến")]
    [Min(0f)] public float hiddenTime = 0.2f;
    [Tooltip("Bóng đen hiện ở điểm đến bao lâu trước khi nó hiện ra chém")]
    [Min(0f)] public float appearTelegraph = 0.15f;
    [Min(0.1f)] public float behindDistance = 1.1f;
    [Tooltip("Bóng đen để lại tại chỗ tàng hình")]
    public GameObject shadowPrefab;

    [Header("Animation Hitbox")]
    [Tooltip("Hitbox keyframe trong clip Attack/Appear (kiểu Knight/SwordAttack)")]
    public Collider2D animationAttackHitbox;

    [Header("Line of Sight")]
    [Tooltip("Layer chặn tầm nhìn. Để trống sẽ tự lấy layer Ground.")]
    public LayerMask sightBlockerMask;
    [Tooltip("Trong tầm này coi như luôn thấy — tránh mép địa hình che linecast khi đứng sát nhau")]
    [Min(0f)] public float pointBlankRange = 1.5f;

    Animator animator;
    Damageable damageable;
    Rigidbody2D rb;
    Collider2D bodyCollider;
    SpriteRenderer spriteRenderer;
    EnemyMove enemyMove;
    EnemySfxController sfx;
    Collider2D currentTarget;
    Coroutine teleportRoutine;
    float teleportTimer;

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
    // SetFloatBehaviour trên state Attack/Appear reset cooldown khi thoát state
    // -> đó chính là "vài giây nghỉ để người chơi đánh nó".
    public float AttackCooldown
    {
        get { return animator.GetFloat(AnimationStrings.attackCooldown); }
        private set { animator.SetFloat(AnimationStrings.attackCooldown, Mathf.Max(value, 0)); }
    }

    private bool IsActing
    {
        get
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            return state.IsName("Attack") || state.IsName("Appear") || state.IsName("Vanish");
        }
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        damageable = GetComponent<Damageable>();
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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
            StopActing();
            return;
        }

        if (AttackCooldown > 0)
            AttackCooldown -= Time.deltaTime;

        if (teleportTimer > 0)
            teleportTimer -= Time.deltaTime;

        currentTarget = GetTarget(attackZone);

        // Đang tàng hình thì không cho animator tự vào đòn chém thường
        HasTarget = currentTarget != null && teleportRoutine == null;

        bool velocityLocked = damageable.LockVelocity;
        bool acting = IsActing;

        if (enemyMove != null)
        {
            enemyMove.shouldMove = !HasTarget && !acting && teleportRoutine == null;
            enemyMove.lockedMove = HasTarget || acting || teleportRoutine != null;
        }

        if ((HasTarget || acting) && !velocityLocked)
            rb.linearVelocityX = 0f;

        // Windup vẫn xoay theo player để không chém hụt sau lưng;
        // hitbox đã bật (đang vung lưỡi) thì khoá hướng lại
        bool strikeActive = animationAttackHitbox != null && animationAttackHitbox.enabled;
        if (HasTarget && !strikeActive && teleportRoutine == null)
            FaceTarget(currentTarget.transform.position);

        // Tiếp cận: thấy player trong tầm xa + đủ điều kiện -> tàng hình ra sau lưng
        if (teleportRoutine == null && teleportTimer <= 0f && AttackCooldown <= 0f && !acting)
        {
            Collider2D farTarget = GetTarget(teleportZone);
            if (farTarget != null && farTarget.GetComponentInParent<PlayerController>() != null)
                teleportRoutine = StartCoroutine(TeleportStrike(farTarget));
        }
    }

    private IEnumerator TeleportStrike(Collider2D target)
    {
        animator.SetTrigger(AnimationStrings.vanishTrigger);
        if (sfx != null)
            sfx.PlayAttackWindup();

        yield return new WaitForSeconds(vanishAnimTime);

        if (!damageable.IsAlive)
        {
            teleportRoutine = null;
            yield break;
        }

        // Để lại bóng đen tại chỗ rồi biến mất hẳn
        if (shadowPrefab != null)
            Instantiate(shadowPrefab, transform.position, Quaternion.identity);

        spriteRenderer.enabled = false;
        if (bodyCollider != null)
            bodyCollider.enabled = false;
        damageable.setInvisibleFrame(hiddenTime + 0.2f);

        yield return new WaitForSeconds(hiddenTime);

        PlayerController player = target != null ? target.GetComponentInParent<PlayerController>() : null;
        if (player != null)
        {
            // Sau lưng = phía ngược hướng player đang nhìn; bám sát chân player
            // để không leo nhầm lên bậc địa hình cao hơn
            float side = player.IsFacingRight ? -1f : 1f;
            Vector2 appearPosition = (Vector2)player.transform.position + new Vector2(side * behindDistance, 0.1f);

            // Né tường: dò cả THÂN người tại chỗ hiện ra, bít sau lưng thì thử trước mặt
            if (SpotBlocked(appearPosition))
                appearPosition = (Vector2)player.transform.position + new Vector2(-side * behindDistance, 0.1f);

            if (!SpotBlocked(appearPosition))
            {
                // Ghim xuống mặt đất cho đứng chuẩn
                RaycastHit2D ground = Physics2D.Raycast(appearPosition + Vector2.up * 0.5f, Vector2.down, 3f, sightBlockerMask);
                if (ground.collider != null)
                    appearPosition.y = ground.point.y + 0.02f;

                // Bóng đen báo hiệu ở điểm đến - vừa không "mất tích", vừa cho player cửa né
                if (shadowPrefab != null)
                    Instantiate(shadowPrefab, appearPosition, Quaternion.identity);

                yield return new WaitForSeconds(appearTelegraph);

                // Teleport qua transform để transform.position cập nhật NGAY,
                // FaceTarget phía dưới mới tính đúng hướng (rb.position cập nhật trễ 1 physics step)
                transform.position = appearPosition;
                Physics2D.SyncTransforms();
            }
            // Cả hai phía đều bít -> đứng nguyên chỗ cũ mà hiện ra, không chui tường

            FaceTarget(player.transform.position);
        }

        spriteRenderer.enabled = true;
        if (bodyCollider != null)
            bodyCollider.enabled = true;

        // Appear = đòn chém (hitbox keyframe trong clip); SetFloatBehaviour trên state
        // Appear reset attackCooldown khi thoát -> hở đòn vài giây cho player phạt
        animator.SetTrigger(AnimationStrings.appearTrigger);
        teleportTimer = teleportCooldown;
        teleportRoutine = null;
    }

    // Animation Event ở frame đầu clip Attack/Appear
    public void AttackWindup()
    {
        if (sfx != null)
            sfx.PlayAttackWindup();
    }

    public void OnHit(int damage, Vector2 hitKnockback)
    {
        // Max thay vì cộng dồn: combo liên tiếp không chồng Y phóng quái lên trời
        rb.linearVelocity = new Vector2(hitKnockback.x, Mathf.Max(rb.linearVelocity.y, hitKnockback.y));

        // Bị đánh khi chưa kịp biến mất -> huỷ tàng hình
        if (teleportRoutine != null && spriteRenderer.enabled)
        {
            StopCoroutine(teleportRoutine);
            teleportRoutine = null;
        }

        // Bị đánh lén: quay về phía kẻ đánh (knockback đẩy ra xa kẻ đánh)
        if (Mathf.Abs(hitKnockback.x) > 0.01f)
            FaceTarget(transform.position + new Vector3(-Mathf.Sign(hitKnockback.x), 0f, 0f));

        if (enemyMove != null)
        {
            enemyMove.BloodEffect(hitKnockback.normalized);
            enemyMove.Flash();
        }
    }

    private Collider2D GetTarget(DetectionZone zone)
    {
        if (zone == null)
            return null;

        for (int i = zone.detectedColliders.Count - 1; i >= 0; i--)
        {
            Collider2D candidate = zone.detectedColliders[i];
            if (candidate == null)
            {
                zone.detectedColliders.RemoveAt(i);
                continue;
            }

            if (candidate.GetComponentInParent<PlayerController>() != null && HasLineOfSight(candidate))
                return candidate;
        }

        return null;
    }

    // Chỗ hiện ra có vướng tường/đất không — dò theo khối thân người, không dò 1 điểm
    private bool SpotBlocked(Vector2 feetPosition)
    {
        return Physics2D.OverlapBox(
            feetPosition + Vector2.up * 0.9f,
            new Vector2(0.7f, 1.6f),
            0f,
            sightBlockerMask) != null;
    }

    // Không cho nhìn xuyên tường: linecast về phía nhân vật, vướng Ground là mất dấu
    private bool HasLineOfSight(Collider2D target)
    {
        Vector2 origin = bodyCollider != null ? (Vector2)bodyCollider.bounds.center : (Vector2)transform.position;

        // Đứng sát nhau thì mép bậc địa hình hay che linecast dù thấy rõ ràng
        if (Vector2.Distance(origin, target.bounds.center) <= pointBlankRange)
            return true;

        if (Physics2D.Linecast(origin, target.bounds.center, sightBlockerMask).collider == null)
            return true;

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

    private void StopActing()
    {
        currentTarget = null;
        DisableAnimationHitbox();

        if (teleportRoutine != null)
        {
            StopCoroutine(teleportRoutine);
            teleportRoutine = null;
        }

        // Chết giữa lúc tàng hình thì phải hiện xác ra
        if (spriteRenderer != null && !spriteRenderer.enabled)
            spriteRenderer.enabled = true;
        if (bodyCollider != null && !bodyCollider.enabled)
            bodyCollider.enabled = true;

        if (enemyMove != null)
        {
            enemyMove.shouldMove = damageable.IsAlive;
            enemyMove.lockedMove = false;
        }
    }

    private void OnDisable()
    {
        currentTarget = null;
        teleportRoutine = null;
        DisableAnimationHitbox();

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
        if (bodyCollider != null)
            bodyCollider.enabled = true;
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
