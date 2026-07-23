using UnityEngine;

public class Attack : MonoBehaviour
{
    // Hệ số quy đổi knockback toàn cục: nhập số nhỏ trong Inspector,
    // lực thật = (x * 6, y * 3). Đổi 2 hằng này là đổi cảm giác knockback cả game.
    public const float knockbackScaleX = 6f;
    public const float knockbackScaleY = 3f;

    public int attackDamage = 10;
    public Vector2 knockback = Vector2.zero;

    public bool turnOffOnAttack = false;
    public bool KnockbackRelativePosition = false;



private void OnTriggerEnter2D(Collider2D collision)
    {
        Damageable damageable = collision.GetComponent<Damageable>();
        if (damageable == null)
            return;

        // Đẩy mục tiêu ra xa người đánh theo vị trí tương đối,
        // không dựa vào localScale vì mỗi sprite sheet quay mặt một hướng khác nhau
        Vector2 attackerPosition = transform.parent != null ? transform.parent.position : transform.position;
        float pushDirection = collision.bounds.center.x >= attackerPosition.x ? 1f : -1f;
        Vector2 deliveredKnockback = new Vector2(
            Mathf.Abs(knockback.x) * knockbackScaleX * pushDirection,
            knockback.y * knockbackScaleY);

        if(KnockbackRelativePosition)
        {
            attackerPosition = transform.root.position;
            float sign = collision.transform.position.x - attackerPosition.x;
            if(sign < 0.0f)
            {
                deliveredKnockback = new Vector2(Mathf.Abs(knockback.x) * knockbackScaleX * -1, knockback.y * knockbackScaleY);
            }else
            {
                deliveredKnockback = new Vector2(Mathf.Abs(knockback.x) * knockbackScaleX, knockback.y * knockbackScaleY);

            }
        }
        bool handled = damageable.Hit(
            attackDamage,
            deliveredKnockback,
            transform.root.position);


        
        if (!handled)
            return;

        // Lớp impact chung cho mọi đòn cận chiến (player lẫn enemy):
        // trúng thịt = hit, bị đỡ = tiếng khiên/vũ khí chạm nhau
        if (damageable.LastHitWasBlocked)
        {
            Sfx.PlayAt(SfxId.CombatBlock, collision.bounds.center);
        }
        else
        {
            Sfx.PlayAt(SfxId.CombatHit, collision.bounds.center);
            Debug.Log(collision.name + " hit for " + attackDamage + " damage!");
        }

        if(turnOffOnAttack)
        {
            PolygonCollider2D poly = GetComponent<PolygonCollider2D>();
            poly.enabled = false;
        }
        
    }
}
