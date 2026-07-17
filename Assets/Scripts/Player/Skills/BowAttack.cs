using UnityEngine;

/// <summary>
/// Chiêu bắn cung (slot 2): dùng animation player_bow qua trigger rangedAttack
/// — state này vào được từ cả mặt đất lẫn trên không nên bắn được ở mọi tư thế.
/// Animation event FireProjectile trong player_bow.anim gọi
/// PlayerController.FireProjectile -> ProjectileLauncher bắn Arrow.prefab.
/// Tốn mana qua PlayerStats.TriggerSlot2 (thiếu mana có tiếng denied).
/// </summary>
public class BowAttack : MonoBehaviour, SpecialSkill
{
    [SerializeField] private float maxCooldown = 1.5f;
    [SerializeField] private int manaCost = 10;
    [SerializeField] private Sprite overView;

    private float cooldown = 999f;

    private void Update()
    {
        cooldown += Time.deltaTime;
        cooldown = Mathf.Clamp(cooldown, 0.0f, maxCooldown);
    }

    public bool Trigger()
    {
        if (cooldown >= maxCooldown)
        {
            cooldown = 0.0f;
            return true;
        }
        return false;
    }

    public float GetMaxCoolDown()
    {
        return maxCooldown;
    }

    public float GetCoolDown()
    {
        return cooldown;
    }

    public string GetAnimationString()
    {
        return AnimationStrings.rangedAttackTrigger;
    }

    public int GetManaCost()
    {
        return manaCost;
    }

    public Sprite GetSprite()
    {
        return overView;
    }
}
