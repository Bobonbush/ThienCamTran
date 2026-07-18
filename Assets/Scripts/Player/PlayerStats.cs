using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    Damageable damagable;

    [SerializeField]
    SkillSlotUI skillSlot;


   


    private int _Mana = 0;
    private int _MaxMana = 100;

    

    private int HealValue = 10;

    private SpecialSkill slot1 = null;
    private SpecialSkill slot2 = null;

    public int Health
    {
        get { return damagable.Health; }
    }

    public int MaxHealth
    {
        get { return damagable.MaxHealth; }
    }

    public int Mana
    {
        set { if (value <= _MaxMana) _Mana = value; else _Mana = _MaxMana; }
        get { return _Mana; }
    }

    public int MaxMana
    {
        set { _MaxMana = value; }
        get { return _MaxMana; }
    }


    public float stamina = 100;
    private float maxStamina = 100;

    private int rateUpdate = 10;

    void Start()
    {
        damagable = GetComponent<Damageable>();

        // Slot 1 (phím A) = bắn cung 10 mana theo thiết kế;
        // slot 2 (phím D) = talisman. Tự thêm BowAttack nếu prefab chưa gắn.
        BowAttack bow = GetComponent<BowAttack>();
        if (bow == null)
            bow = gameObject.AddComponent<BowAttack>();
        slot1 = bow;
        slot2 = GetComponent<ThrowTalisman>();

        SetSkillAvatar();
    }

    private void Update()
    {
        stamina += (int) 10 * Time.deltaTime;
        stamina = Mathf.Clamp(stamina, 0, maxStamina);
    }

    public bool CanConsume(float stamina_value)
    {
        if (stamina_value > stamina)
        {
            Sfx.Play(SfxId.UiDenied);
            return false;
        }
        stamina -= stamina_value;
        return true;
    }

    public bool ConsumeMana(int mana_value)
    {
        if (mana_value > Mana)
        {
            Sfx.Play(SfxId.UiDenied);
            return false;
        }
        Mana -= mana_value;
        return true;
    }

    private void SetSkillAvatar()
    {
        if (skillSlot == null)
        {
            return;
        }

        skillSlot.SetAvatar(slot1, slot2);
    }


    public bool CanHeal()
    {
        bool yesOrNo = (Mana == MaxMana);
        if(yesOrNo)
        {
            Sfx.Play(SfxId.UiDenied);
        }
        return yesOrNo;
    }


   

    public void OnHealing()
    {
        damagable.Health += HealValue;
        Mana = 0;
        Sfx.Play(SfxId.PlayerHeal);
    }


    public void TriggerSlot1(Animator animator)
    {
        if (slot1 == null) return;

        if (!ConsumeMana(slot1.GetManaCost()))
            return;

        if(slot1.Trigger())
        {
            animator.SetTrigger(slot1.GetAnimationString());
            Sfx.Play(slot1.GetCastSfxId());
        }
    }

    public void TriggerSlot2(Animator animator)
    {
        if (slot2 == null) return;

        if (!ConsumeMana(slot2.GetManaCost()))
            return;

        if (slot2.Trigger())
        {
            animator.SetTrigger(slot2.GetAnimationString());
            Sfx.Play(slot2.GetCastSfxId());
        }
    }
}
