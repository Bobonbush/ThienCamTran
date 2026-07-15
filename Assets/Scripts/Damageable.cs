using System;
using UnityEngine;
using UnityEngine.Events;
public class Damageable : MonoBehaviour
{
    public UnityEvent<int, Vector2> damageableHit = new UnityEvent<int, Vector2>();
    Animator animator;
    private IDirectionalDamageBlocker directionalDamageBlocker;

    public bool LastHitWasBlocked { get; private set; }



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

    [Tooltip("Thời gian tối thiểu knockback được giữ sau khi trúng đòn, kể cả khi animator rời state hit sớm (vd đang mash attack)")]
    public float minKnockbackLockTime = 0.25f;
    private float velocityLockUntil;

    [Header("Poise")]
    [Tooltip("Gan lì: 0 = bật đầy đủ theo lực đánh, 1 = đứng trơ không xê dịch")]
    [Range(0f, 1f)] public float knockbackResistance = 0f;

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
            // Animator giữ khoá theo state hit; timer đảm bảo knockback sống tối thiểu
            // minKnockbackLockTime kể cả khi state hit bị trigger khác cắt sớm
            return animator.GetBool(AnimationStrings.lockVelocity) || Time.time < velocityLockUntil;
        }
        private set
        {
            animator.SetBool(AnimationStrings.lockVelocity, value);
            if (value)
                velocityLockUntil = Time.time + minKnockbackLockTime;
        }
    }

public void Awake()
    {
        animator = GetComponent<Animator>();
        animator.SetBool(AnimationStrings.isAlive, _isAlive);

        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            IDirectionalDamageBlocker blocker = behaviours[i] as IDirectionalDamageBlocker;
            if (blocker == null)
                continue;

            directionalDamageBlocker = blocker;
            break;
        }
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
        return ApplyHit(damage, knockback, null);
    }

    public bool Hit(int damage, Vector2 knockback, Vector2 damageSource)
    {
        return ApplyHit(damage, knockback, damageSource);
    }

    private bool ApplyHit(int damage, Vector2 knockback, Vector2? damageSource)
    {
        LastHitWasBlocked = false;

        if (!IsAlive || isInvincible)
            return false;

        if (damageSource.HasValue &&
            directionalDamageBlocker != null &&
            directionalDamageBlocker.BlocksDamageFrom(damageSource.Value))
        {
            LastHitWasBlocked = true;
            directionalDamageBlocker.OnDamageBlocked(damageSource.Value);
            return true;
        }

        Health -= damage;
        isInvincible = true;

        // Poise: giảm lực knockback ngay tại nguồn, mọi listener nhận giá trị đã trừ kháng
        if (knockbackResistance > 0f)
            knockback *= 1f - knockbackResistance;

        animator.SetTrigger(AnimationStrings.hitTrigger);
        LockVelocity = true;
        damageableHit?.Invoke(damage, knockback);
        CharacterEvents.characterDamaged?.Invoke(gameObject, damage);

        return true;
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
