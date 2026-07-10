using System;
using UnityEngine;
using UnityEngine.Events;
public class Damageable : MonoBehaviour
{
    public UnityEvent<int, Vector2> damageableHit;
    Animator animator;


    [SerializeField]
    private int _maxHealth = 100;

    [SerializeField]
    private bool canRevive = false;

    public int MaxHealth
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
    private int _health = 100;

    public int Health
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
    private bool deathByTrap = false;

    private bool _ReviveAction = false;
    
    public bool ReviveAction
    {
        get { return _ReviveAction; }
        set { _ReviveAction = value; }
    } 

    private float timeSinceHit = 0;
    public float invicibilityTimer = 0.5f;

    private float timeInvisibleFrame = 1.0f;
    private float maxTimeInvisibleFrame = 0.0f;

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
                if (canRevive && deathByTrap)
                {
                    deathByTrap = false;
                    ReviveAction = true;    // Set to false by playerController
                }
            }


            if(timeInvisibleFrame > maxTimeInvisibleFrame)
            {
                isInvincible = false;
                
                if (canRevive && deathByTrap)
                {
                    deathByTrap = false;
                    ReviveAction = true;    // Set to false by playerController
                }
            }

            timeSinceHit += Time.deltaTime;
            timeInvisibleFrame += Time.deltaTime;
        }

    }


    public void setInvisibleFrame(float duration)
    {
        isInvincible = true;
        timeInvisibleFrame = 0.0f;
        maxTimeInvisibleFrame = duration;
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
            
            CharacterEvents.characterDamaged?.Invoke(gameObject, damage);

            return true;
        }
        return false;
    }

    public bool HitTrap()
    {
        // Falling to a trap need to reset the position and minus a constant health
        
        if(IsAlive && !isInvincible)
        {
            int CLostHP = 10;
            if (!canRevive) CLostHP = Health;    // if it is a mobs not player no need to revive
            Health -= CLostHP;
            

            isInvincible = true;

            deathByTrap = true;

            animator.SetTrigger(AnimationStrings.hitTrigger);
            LockVelocity = true;
            damageableHit?.Invoke( (int) CLostHP, Vector2.zero);

            return true;
        }

        return false;
    }

    public bool Heal(int healthRestore) 
    { 
        if (IsAlive && Health < MaxHealth)
        {
            int maxHeal = (int) Mathf.Max(MaxHealth - Health, 0);
            int actualHeal = Mathf.Min(healthRestore, maxHeal);
            Health += actualHeal;
            CharacterEvents.characterHealed(gameObject, actualHeal);
            return true;
        }
        return false;
    }
}
