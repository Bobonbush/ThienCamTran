using System;
using UnityEngine;
using UnityEngine.Events;
public class Damageable : MonoBehaviour
{
    public UnityEvent<int, Vector2> damageableHit;
    Animator animator;


    [SerializeField]
    private float _maxHealth = 100;

    [SerializeField]
    private bool canRevive = false;

    public float MaxHealth
    {
        get
        {
            return _maxHealth; 
        }
        set
        {
            _maxHealth = value;
        }
    }

    [SerializeField]
    private float _health = 100;

    public float Health
    {
        get
        {
            return _health;
        }
        set
        {
            _health = value;
            if (_health <= 0 && IsAlive)
            {
                IsAlive = false;
            }
        }
    }
    [SerializeField]
    private bool _isAlive = true;

    [SerializeField]
    private bool isInvincible = false;

    private bool _ReviveAction = false;
    
    public bool ReviveAction
    {
        get { return _ReviveAction; }
        set { _ReviveAction = value; }
    } 

    private float timeSinceHit = 0;
    public float invicibilityTimer = 0.5f;

    public bool IsAlive
    {
        get
        {
            return _isAlive;
        }
        set
        {
            _isAlive = value;
            animator.SetBool(AnimationStrings.isAlive, value);
            Debug.Log("IsAlive set to: " + value);
        }
    }
    // 
    public bool LockVelocity
    {
        get
        {
            return animator.GetBool(AnimationStrings.lockVelocity);
        }
        private set
        {
            animator.SetBool(AnimationStrings.lockVelocity, value);
        }
    }

    public void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (isInvincible)
        {
            if (timeSinceHit > invicibilityTimer)
            {
                isInvincible = false;
                timeSinceHit = 0;
                if(canRevive)
                    ReviveAction = true;    // Set to false by playerController
            }

            timeSinceHit += Time.deltaTime;
        }

    }

    
    public bool Hit(int damage, Vector2 knockback)
    {
        if (IsAlive && !isInvincible)
        {
            Health -= damage;
            isInvincible = true;

            // Notify other subscribed components that the damageable was hit to handle the knockback and such
            animator.SetTrigger(AnimationStrings.hitTrigger);
            LockVelocity = true;
            damageableHit?.Invoke(damage, knockback);
            CharacterEvents.characterDamaged.Invoke(gameObject, damage);

            return true;
        }
        return false;
    }

    public bool HitTrap()
    {
        // Falling to a trap need to reset the position and minus a constant health
        
        if(IsAlive )
        {
            float CLostHP = 10;
            if (!canRevive) CLostHP = Health;    // if it is a mobs not player no need to revive
            Health -= CLostHP;
            

            isInvincible = true;

            animator.SetTrigger(AnimationStrings.hitTrigger);
            LockVelocity = true;
            damageableHit?.Invoke( (int) CLostHP, Vector2.zero);

            return true;
        }

        return false;
    }
}
