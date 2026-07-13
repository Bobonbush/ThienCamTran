using UnityEngine;

public class ProjectileTrajectory : MonoBehaviour
{
    public int damage = 10;

    public Vector2 knockback = new Vector2(10f, 0);
    public bool damagesPlayerOnly = false;

    public bool impactOnGround = false;

    [Header("Audio")]
    public AudioClip[] impactClips;
    [Range(0f, 1f)] public float impactVolume = 0.9f;

    public float fall = 0f; // Hi?u ?ng c?m xu?ng d?t
    public float maxLifetime = 5f;   // t? hu? n?u bay m�i kh�ng tr�ng g�
    [SerializeField]
    private float offsetRotation = 0.0f;  // Set the origin object to lie on the x axis

    Rigidbody2D rb;
    Animator animator;       // c� th? null (d?n sprite tinh nhu Arrow)
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
        if (hasLaunchVelocity)
            rb.linearVelocity = launchVelocity;
        
        Destroy(gameObject, maxLifetime);
    }

    public void Launch(Vector2 velocity)
    {
        hasLaunchVelocity = true;
        launchVelocity = velocity;

        if (rb != null)
            rb.linearVelocity = velocity;
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
                //Debug.Log(collision.name + " hit for " + damage + " damage!");
                Impact();
            }
        }else if(collision.GetComponentInParent<Ground>() != null)
        {
            if (impactOnGround)
            {
                Impact();
                return;
            }

            hasImpacted = true;
            PlayImpactSfx();

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

    // D?ng d?n l?i v� choi animation n?; n?u kh�ng c� Animator th� hu? ngay (gi? h�nh vi cu c?a Arrow)
    private void Impact()
    {
        hasImpacted = true;
        PlayImpactSfx();
        rb.linearVelocity = Vector2.zero;
        if (col != null) col.enabled = false;   // kh�ng tr�ng th�m l?n n?a

        if (animator != null)
        {
            animator.SetTrigger(AnimationStrings.hitTrigger);   // -> state Impact
            // Hu? object do Animation Event ? cu?i clip Impact g?i DestroySelf(),
            // ho?c g?n FadeRemoveBehaviour l�n state Impact.
        }
        else
        {

            Destroy(gameObject);
        }
    }

    // G?i t? Animation Event ? frame cu?i clip Impact
    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    private void PlayImpactSfx()
    {
        EnemySfxController.PlayAtPoint(impactClips, transform.position, impactVolume);
    }
}
