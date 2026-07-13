using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int damage = 10;
    public Vector2 moveSpeed = new Vector2(3f, 0);
    public Vector2 knockback = new Vector2(0, 0);
    public float maxLifetime = 5f;   // tự huỷ nếu bay mãi không trúng gì

    public float fall = 0f;

    Rigidbody2D rb;
    Animator animator;       // có thể null (đạn sprite tĩnh như Arrow)
    Collider2D col;
    bool hasImpacted = false;
    bool hasLaunchVelocity = false;
    Vector2 launchVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
    }

    void Start()
    {
        rb.linearVelocity = hasLaunchVelocity ? launchVelocity : new Vector2(moveSpeed.x * transform.localScale.x, moveSpeed.y);
        Destroy(gameObject, maxLifetime);
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

        Damageable damageable = collision.GetComponent<Damageable>();

        if (damageable != null)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            float dir = rb.linearVelocityX;
            Vector2 deliveredKnockback = dir > 0 ? knockback : new Vector2(-knockback.x, knockback.y);
            // Hit the damageable object
            bool gotHit = damageable.Hit(damage, deliveredKnockback);
            if (gotHit)
            {
                Debug.Log(collision.name + " hit for " + damage + " damage!");
                Impact();
            }
        }
        else
        {
            // Touch ground



            BoxCollider2D bx = GetComponent<BoxCollider2D>();
            bx.enabled = false;
            Rigidbody2D rb = GetComponent<Rigidbody2D>();

            Vector2 direction = Vector2.Normalize(rb.linearVelocity);
            rb.bodyType = RigidbodyType2D.Kinematic;


            Vector3 position = transform.position;
            position.x += direction.x * fall;
            position.y += direction.y * fall;

            transform.position = position;

            rb.linearVelocity = Vector2.zero;
        }
    }

    // Dừng đạn lại và chơi animation nổ; nếu không có Animator thì huỷ ngay (giữ hành vi cũ của Arrow)
    private void Impact()
    {
        hasImpacted = true;
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
}
