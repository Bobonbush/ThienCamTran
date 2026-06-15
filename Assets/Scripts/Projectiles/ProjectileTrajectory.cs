using NUnit.Framework.Constraints;
using UnityEngine;

public class ProjectileTrajectory : MonoBehaviour
{
    public int damage = 10;

    public Vector2 knockback = new Vector2(10f, 0);
    public float fall = 0f; // Hiệu ứng cắm xuống đất
    public float maxLifetime = 5f;   // tự huỷ nếu bay mãi không trúng gì
    [SerializeField]
    private float offsetRotation = 0.0f;  // Set the origin object to lie on the x axis

    Rigidbody2D rb;
    Animator animator;       // có thể null (đạn sprite tĩnh như Arrow)
    Collider2D col;
    bool hasImpacted = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
    }

    void Start()
    {

        
        Destroy(gameObject, maxLifetime);
    }

    private void Update()
    {

        if (rb.linearVelocityY == 0) return;
        // indicate the direction of the object toward the trajection
        float angle = offsetRotation + Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        Vector3 currentRotation = transform.eulerAngles;
        currentRotation.z = angle;
        transform.eulerAngles = currentRotation;

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
                //Debug.Log(collision.name + " hit for " + damage + " damage!");
                Impact();
            }
        }else if(collision.GetComponent<Ground>() != null)
        {
            // Touch ground

            

            BoxCollider2D bx = GetComponent<BoxCollider2D>();
            bx.enabled = false;
            Rigidbody2D rb = GetComponent<Rigidbody2D>();

            Vector2 direction = Vector2.Normalize(rb.linearVelocity);
            rb.isKinematic = true;


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
