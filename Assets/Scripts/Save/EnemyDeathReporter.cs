using UnityEngine;

/// <summary>
/// SaveManager tự gắn component này lên mọi quái thường (EnemyMove) khi scene
/// load — KHÔNG cần thêm vào prefab. Khi quái chết, ghi vào dữ liệu tạm để
/// chuyển scene qua lại quái vẫn chết; nghỉ ở Save Zone thì dữ liệu tạm bị
/// xoá và quái hồi sinh (khớp với SaveZone.RespawnRegularEnemies).
/// </summary>
public class EnemyDeathReporter : MonoBehaviour
{
    private string sceneName;
    private string enemyId;
    private Damageable damageable;
    private bool reported;

    public void Init(string scene, string id)
    {
        sceneName = scene;
        enemyId = id;

        damageable = GetComponent<Damageable>();
        if (damageable != null)
            damageable.damageableHit.AddListener(OnDamaged);
    }

    private void OnDestroy()
    {
        if (damageable != null)
            damageable.damageableHit.RemoveListener(OnDamaged);
    }

    private void OnDamaged(int damage, Vector2 knockback)
    {
        if (reported || damageable.IsAlive)
            return;

        reported = true;
        SaveManager.Instance.MarkEnemyDead(sceneName, enemyId);
    }


    public void ResetReport()
    {
        reported = false;
    }
}
