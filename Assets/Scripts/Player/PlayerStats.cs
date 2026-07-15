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
        slot1 = GetComponent<ThrowTalisman>();

        SetSkillAvatar();
    }

    private void Update()
    {
        stamina += (int) 10 * Time.deltaTime;
        stamina = Mathf.Clamp(stamina, 0, maxStamina);
    }

    public bool CanConsume(float stamina_value)
    {
        if (stamina_value > stamina) return false;

        stamina -= stamina_value;
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
        return Mana == MaxMana;
    }


    public void Heal()
    {
        if (damagable == null)
        {
            return;
        }


        damagable.Health += HealValue;
        
    }

    public void OnHeal()
    {
        damagable.Health += HealValue;
        Mana = 0;
    }


    public void TriggerSlot1(Animator animator)
    {
        if (slot1 == null) return;
        
        if(slot1.GetManaCost() > Mana)
        {
            return;
        }

        Mana -= slot1.GetManaCost();

        if(slot1.Trigger())
        {
            animator.SetTrigger(slot1.GetAnimationString());
        }
    }

    public void TriggerSlot2(Animator animator)
    {
        if (slot2 == null) return;

        if (slot2.GetManaCost() > Mana)
        {
            return;
        }

        Mana -= slot2.GetManaCost();

        if (slot2.Trigger())
        {
            animator.SetTrigger(slot2.GetAnimationString());
        }
    }
}
