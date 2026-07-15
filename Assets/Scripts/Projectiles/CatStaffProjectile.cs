using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class CatStaffProjectile : MonoBehaviour
{
    public int damage = 12;
    public Vector2 knockback = new Vector2(3f, 2f);
    public bool damagesPlayerOnly = true;
    [Min(0.1f)] public float outwardDistance = 5.5f;
    [Min(0.1f)] public float returnSpeed = 14f;
    public float spinSpeed = -720f;
    [Min(0.1f)] public float maxLifetime = 5f;
    [Min(0.05f)] public float catchDistance = 0.35f;

    [Header("Audio")]
    public AudioClip[] impactClips;
    [Range(0f, 1f)] public float impactVolume = 0.85f;

    private readonly HashSet<int> hitTargets = new HashSet<int>();
    private Rigidbody2D rb;
    private RangeEnemy owner;
    private Vector2 launchPosition;
    private bool returning;
    private bool returnNotified;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }

    private void Start()
    {
        Destroy(gameObject, maxLifetime);
    }

    public void Launch(RangeEnemy projectileOwner, Vector2 direction, float speed)
    {
        owner = projectileOwner;
        launchPosition = transform.position;

        // Bay đúng hướng ngắm (chủ nhân đã nhắm thẳng đầu player), không ép ngang nữa
        rb.linearVelocity = direction.normalized * speed;
    }

    private void FixedUpdate()
    {
        rb.MoveRotation(rb.rotation + spinSpeed * Time.fixedDeltaTime);

        if (owner == null || !owner.isActiveAndEnabled)
        {
            Destroy(gameObject);
            return;
        }

        if (!returning)
        {
            if (((Vector2)transform.position - launchPosition).sqrMagnitude >= outwardDistance * outwardDistance)
                returning = true;

            return;
        }

        Vector2 toOwner = owner.StaffReturnPosition - rb.position;
        if (toOwner.sqrMagnitude <= catchDistance * catchDistance)
        {
            NotifyReturned();
            Destroy(gameObject);
            return;
        }

        rb.linearVelocity = toOwner.normalized * returnSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Damageable target = collision.GetComponentInParent<Damageable>();
        if (target == null)
            return;

        if (damagesPlayerOnly && collision.GetComponentInParent<PlayerController>() == null)
            return;

        if (!hitTargets.Add(target.GetInstanceID()))
            return;

        float direction = rb.linearVelocityX >= 0f ? 1f : -1f;
        bool gotHit = target.Hit(damage, new Vector2(knockback.x * direction, knockback.y), transform.position);
        if (gotHit)
            EnemySfxController.PlayAtPoint(impactClips, transform.position, impactVolume);
    }

    private void NotifyReturned()
    {
        if (returnNotified)
            return;

        returnNotified = true;
        if (owner != null)
            owner.OnStaffReturned();
    }

    private void OnDestroy()
    {
        if (!returnNotified && owner != null && owner.isActiveAndEnabled)
            NotifyReturned();
    }
}
