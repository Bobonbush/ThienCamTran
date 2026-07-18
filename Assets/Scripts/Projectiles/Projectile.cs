using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int damage = 10;
    public Vector2 moveSpeed = new Vector2(3f, 0);
    public Vector2 knockback = new Vector2(0, 0);
    public float maxLifetime = 5f;   // tự huỷ nếu bay mãi không trúng gì

    public bool damagesPlayerOnly = false;
    public bool piercesWalls = false;   // đạn bay xuyên Ground thay vì ghim vào tường
    public float fall = 0f;

    [Header("Audio")]
    public AudioClip[] impactClips;
    [Range(0f, 1f)] public float impactVolume = 0.8f;

    Rigidbody2D rb;
    Animator animator;       // có thể null (đạn sprite tĩnh như Arrow)
    Collider2D col;
    bool hasImpacted = false;
    bool hasLaunchVelocity = false;
    Vector2 launchVelocity;
    Vector2 lastPosition;

    private static int wallMask = -1;
    private static int WallMask
    {
        get
        {
            if (wallMask < 0)
                wallMask = LayerMask.GetMask("Ground");
            return wallMask;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
    }

    void Start()
    {
        rb.linearVelocity = hasLaunchVelocity ? launchVelocity : new Vector2(moveSpeed.x * transform.localScale.x, moveSpeed.y);
        lastPosition = transform.position;
        Destroy(gameObject, maxLifetime);
    }

    // Đạn nhanh (mũi tên 15 u/s) có thể tunnel qua tường mỏng giữa hai physics
    // step — linecast quãng vừa bay để chặn chắc chắn
    private void FixedUpdate()
    {
        if (hasImpacted || piercesWalls)
        {
            lastPosition = transform.position;
            return;
        }

        RaycastHit2D hit = Physics2D.Linecast(lastPosition, transform.position, WallMask);
        if (hit.collider != null)
        {
            transform.position = hit.point;
            Impact();
        }

        lastPosition = transform.position;
    }

    public void Launch(Vector2 velocity)
    {
        hasLaunchVelocity = true;
        launchVelocity = velocity;

        if (rb != null)
            rb.linearVelocity = velocity;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasImpacted) return;

        Damageable damageable = collision.GetComponentInParent<Damageable>();

        if (damageable != null && damagesPlayerOnly && collision.GetComponentInParent<PlayerController>() == null)
            return;

        if (damageable != null)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            float dir = rb.linearVelocityX;
            Vector2 deliveredKnockback = dir > 0 ? knockback : new Vector2(-knockback.x, knockback.y);
            // Hit the damageable object
            bool gotHit = damageable.Hit(damage, deliveredKnockback, transform.position);
            if (gotHit)
            {
                if (!damageable.LastHitWasBlocked)
                    Debug.Log(collision.name + " hit for " + damage + " damage!");
                Impact();
            }
        }
        else if (IsWall(collision))
        {
            // Chạm tường/đất -> biến mất (Impact chơi sfx + anim nổ nếu có).
            // Bản cũ ghim đạn vào tường nhưng GetComponent<BoxCollider2D> null
            // với Arrow (CircleCollider) -> NRE giữa hàm, đạn thành "ma" bay
            // xuyên mọi thứ.
            if (piercesWalls)
                return;

            Impact();
        }
    }

    // Tường = layer Ground/Slidable HOẶC có component Ground —
    // tilemap nào thiếu script Ground vẫn chặn được đạn
    private static bool IsWall(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & WallMask) != 0)
            return true;
        return collision.GetComponentInParent<Ground>() != null;
    }

    // Dừng đạn lại và chơi animation nổ; nếu không có Animator thì huỷ ngay (giữ hành vi cũ của Arrow)
    private void Impact()
    {
        hasImpacted = true;
        PlayImpactSfx();
        rb.linearVelocity = Vector2.zero;
        if (col != null) col.enabled = false;   // không trúng thêm lần nữa

        if (animator != null)
        {
            animator.SetTrigger(AnimationStrings.hitTrigger);   // -> state Impact
            // Huỷ object do Animation Event ở cuối clip Impact gọi DestroySelf(),
            // hoặc gắn FadeRemoveBehaviour lên state Impact.
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Gọi từ Animation Event ở frame cuối clip Impact
    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    private void PlayImpactSfx()
    {
        EnemySfxController.PlayAtPoint(impactClips, transform.position, impactVolume);
    }
}
