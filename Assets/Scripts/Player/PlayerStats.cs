using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public event Action Changed;

    private Damageable damageable;
    [SerializeField] private SkillSlotUI skillSlot;
    [SerializeField] private int mana;
    [SerializeField] private int maxMana = 100;
    [SerializeField] private float stamina = 100f;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private int healValue = 10;

    private SpecialSkill slot1;
    private SpecialSkill slot2;
    private int baseMaxHealth;
    private int baseMaxMana;
    private float baseMaxStamina;
    private float staminaRegenMultiplier = 1f;
    private float staminaCostMultiplier = 1f;
    private float manaGainMultiplier = 1f;
    private float healingMultiplier = 1f;

    public int Health => damageable != null ? damageable.Health : 0;
    public int MaxHealth => damageable != null ? damageable.MaxHealth : 1;
    public int Mana
    {
        get => mana;
        set { mana = Mathf.Clamp(value, 0, maxMana); Changed?.Invoke(); }
    }
    public int MaxMana
    {
        get => maxMana;
        set { maxMana = Mathf.Max(1, value); mana = Mathf.Min(mana, maxMana); Changed?.Invoke(); }
    }
    public float Stamina => stamina;
    public float MaxStamina => maxStamina;
    public float MaxHealthScale => baseMaxHealth > 0 ? (float)MaxHealth / baseMaxHealth : 1f;

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
        baseMaxHealth = damageable != null ? damageable.MaxHealth : 100;
        baseMaxMana = maxMana;
        baseMaxStamina = maxStamina;
    }

    private void Start()
    {
        slot1 = GetComponent<ThrowTalisman>();
        if (skillSlot != null)
            skillSlot.SetAvatar(slot1, slot2);
        Changed?.Invoke();
    }

    private void Update()
    {
        float oldStamina = stamina;
        stamina = Mathf.Clamp(stamina + 10f * staminaRegenMultiplier * Time.deltaTime, 0f, maxStamina);
        if (!Mathf.Approximately(oldStamina, stamina))
            Changed?.Invoke();
    }

    public bool CanConsume(float staminaValue)
    {
        staminaValue *= staminaCostMultiplier;
        if (staminaValue > stamina)
        {
            Sfx.Play(SfxId.UiDenied);
            return false;
        }
        stamina -= staminaValue;
        Changed?.Invoke();
        return true;
    }

    public bool CanHeal()
    {
        bool canHeal = Mana == MaxMana && healingMultiplier > 0f;
        if (!canHeal) Sfx.Play(SfxId.UiDenied);
        return canHeal;
    }

    public void OnHealing()
    {
        if (damageable != null)
            damageable.Heal(Mathf.RoundToInt(healValue * healingMultiplier));
        Mana = 0;
        Sfx.Play(SfxId.PlayerHeal);
    }

    public void GainManaFromHit(int baseAmount)
    {
        Mana += Mathf.RoundToInt(baseAmount * manaGainMultiplier);
    }

    public bool ConsumeFood(ItemData food)
    {
        if (food == null || food.category != Item.Category.Food || damageable == null)
            return false;
        bool canUse = (food.healthRestore > 0 && damageable.Health < damageable.MaxHealth && healingMultiplier > 0f) ||
                      (food.manaRestore > 0 && Mana < MaxMana) ||
                      (food.staminaRestore > 0 && stamina < maxStamina);
        if (!canUse)
        {
            Sfx.Play(SfxId.UiDenied);
            return false;
        }
        if (food.healthRestore > 0 && healingMultiplier > 0f)
            damageable.Heal(Mathf.RoundToInt(food.healthRestore * healingMultiplier));
        Mana += food.manaRestore;
        stamina = Mathf.Min(maxStamina, stamina + food.staminaRestore);
        Changed?.Invoke();
        Sfx.Play(SfxId.UiConfirm);
        return true;
    }

    public void ApplyEquipment(ItemData[] buffs)
    {
        float health = 1f, manaMax = 1f, staminaMax = 1f;
        staminaRegenMultiplier = staminaCostMultiplier = manaGainMultiplier = healingMultiplier = 1f;
        if (buffs != null)
        {
            foreach (ItemData buff in buffs)
            {
                if (buff == null) continue;
                health *= buff.maxHealthMultiplier;
                manaMax *= buff.maxManaMultiplier;
                staminaMax *= buff.maxStaminaMultiplier;
                staminaRegenMultiplier *= buff.staminaRegenMultiplier;
                staminaCostMultiplier *= buff.staminaCostMultiplier;
                manaGainMultiplier *= buff.manaGainMultiplier;
                healingMultiplier *= buff.healingMultiplier;
            }
        }
        if (damageable != null)
        {
            int oldMax = damageable.MaxHealth;
            damageable.MaxHealth = Mathf.Max(1, Mathf.RoundToInt(baseMaxHealth * health));
            damageable.Health = Mathf.Clamp(damageable.Health + damageable.MaxHealth - oldMax, 1, damageable.MaxHealth);
        }
        MaxMana = Mathf.RoundToInt(baseMaxMana * manaMax);
        maxStamina = Mathf.Max(1f, baseMaxStamina * staminaMax);
        stamina = Mathf.Min(stamina, maxStamina);
        Changed?.Invoke();
    }

    public void TriggerSlot1(Animator animator)
    {
        if (slot1 == null || slot1.GetManaCost() > Mana) { Sfx.Play(SfxId.UiDenied); return; }
        Mana -= slot1.GetManaCost();
        if (slot1.Trigger()) { animator.SetTrigger(slot1.GetAnimationString()); Sfx.Play(SfxId.PlayerSkillCast); }
    }

    public void TriggerSlot2(Animator animator)
    {
        if (slot2 == null || slot2.GetManaCost() > Mana) { Sfx.Play(SfxId.UiDenied); return; }
        Mana -= slot2.GetManaCost();
        if (slot2.Trigger()) { animator.SetTrigger(slot2.GetAnimationString()); Sfx.Play(SfxId.PlayerSkillCast); }
    }
}
