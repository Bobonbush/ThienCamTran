using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    Damageable damagable;

    [SerializeField]
    SkillSlotUI skillSlot;


    private int _Mana = 0;
    private int _MaxMana = 100;

    private int _Tre = 3;
    private int _MaxTre = 3;

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
        set {if(value <= _MaxMana) _Mana = value; }
        get { return _Mana; }
    }

    public int MaxMana
    {
        set { _MaxMana = value; }
        get { return _MaxMana; }
    }
    

    public int Tre
    {
        set { _Tre = value; _Tre = Mathf.Clamp(_Tre, 0, _MaxTre); }
        get { return _Tre; }
    }

    public int MaxTre
    {
        set { _MaxTre = value; }
        get { return _MaxTre; }
    }

    void Start()
    {
        damagable = GetComponent<Damageable>();
        slot1 = GetComponent<ThrowTalisman>();

        SetSkillAvatar();
    }

    private void SetSkillAvatar()
    {
        if (skillSlot == null)
        {
            return;
        }

        skillSlot.SetAvatar(slot1, slot2);
    }

    public void Heal()
    {
        if (damagable == null)
        {
            return;
        }

        if (_Tre > 0)
        {
            _Tre--;
            damagable.Health += HealValue;
        }
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
