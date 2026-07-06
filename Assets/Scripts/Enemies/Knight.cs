using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Damageable))]
public class Knight : MonoBehaviour
{
    public float walkAcceleration = 50f;
    public float maxSpeed = 3f;
    public float walkStopRate = 0.1f;




    Rigidbody2D rb;
    TouchingDirections touchingDirections;
    Animator animator;
    Damageable damageable;

    MeleeEnemy melee;



    public float AttackCooldown { 
        get 
        {
            return animator.GetFloat(AnimationStrings.attackCooldown); 
        } 
        private set 
        { 
            animator.SetFloat(AnimationStrings.attackCooldown, Mathf.Max(value, 0)); 
        } 
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        touchingDirections = GetComponent<TouchingDirections>();
        animator = GetComponent<Animator>();
        damageable = GetComponent<Damageable>();
        melee = GetComponent<MeleeEnemy>();

    }

    void Update()
    {
        
        if (AttackCooldown > 0)
        {
            AttackCooldown -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        
    }



}
