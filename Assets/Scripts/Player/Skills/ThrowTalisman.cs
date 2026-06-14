using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;


public class ThrowTalisman : MonoBehaviour, SpecialSkill
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created



    float m_Cooldown = 3.0f;
    float m_MaxCoolDown = 3.0f;

    string animatorString = AnimationStrings.rangedAttackTrigger;

    [SerializeField]
    Sprite overView;

    void Update()
    {
        m_Cooldown += Time.deltaTime;
        m_Cooldown = Mathf.Clamp(m_Cooldown, 0.0f, m_MaxCoolDown);
    }

    public bool Trigger()
    {
        if(m_Cooldown >= m_MaxCoolDown)
        {
            m_Cooldown = 0.0f;
            return true;
        }
        return false;
    }

    public float GetMaxCoolDown()
    {
        return m_MaxCoolDown;
    }

    public float GetCoolDown()
    {
        return m_Cooldown;
    }

    public string GetAnimationString()
    {
        return animatorString;
    }

    public int GetManaCost()
    {
        return 15;
    } 

    public Sprite GetSprite()
    {
        return overView;
    }
}
