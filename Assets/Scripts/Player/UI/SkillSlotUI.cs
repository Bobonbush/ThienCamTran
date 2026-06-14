using UnityEngine;
using UnityEngine.UI;
public class SkillSlotUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    [SerializeField]
    Image slot1;

    [SerializeField]
    Image slot2;


    public void SetAvatar(SpecialSkill skill1, SpecialSkill skill2)
    {
        if(skill1 != null)
        {
            slot1.enabled = true;
            slot1.sprite = skill1.GetSprite();
        } else
        {
            slot1.enabled = false;
        }

        if(skill2 != null)
        {
            slot2.enabled = true;
            slot2.sprite = skill2.GetSprite();
        }else
        {
            slot2.enabled = false;
        }
    }
}
