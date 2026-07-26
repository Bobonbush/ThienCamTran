using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loot rơi từ enemy: kéo item prefab vào danh sách trong Inspector, khi enemy
/// chết (Damageable hết máu) đồ tự văng ra xung quanh giống lúc mở rương.
/// Mỗi entry có số lượng và tỷ lệ rơi riêng.
/// </summary>
[RequireComponent(typeof(Damageable))]
public class EnemyDrops : MonoBehaviour
{
    [System.Serializable]
    public class DropInfo
    {
        public Item itemPrefab;
        [Min(1)] public int count = 1;
        [Tooltip("Xác suất rơi cho TỪNG bản sao: 1 = chắc chắn, 0.3 = 30% mỗi cái")]
        [Range(0f, 1f)] public float chance = 1f;
    }

    [Header("Drops")]
    public List<DropInfo> drops = new List<DropInfo>();

    [Header("Burst")]
    [Tooltip("Điểm văng đồ; bỏ trống sẽ dùng vị trí enemy")]
    public Transform dropPoint;
    public float burstForce = 4f;
    public float upwardForce = 3f;
    public float spreadX = 0.6f;
    public float spreadY = 0.3f;

    private Damageable damageable;
    private bool dropped;

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
    }

    private void OnEnable()
    {
        damageable.damageableHit.AddListener(OnDamaged);
    }

    private void OnDisable()
    {
        damageable.damageableHit.RemoveListener(OnDamaged);
    }

    private void OnDamaged(int damage, Vector2 knockback)
    {
        if (dropped || damageable.IsAlive)
            return;

        dropped = true;
        DropAll();
    }

    private void DropAll()
    {
        Vector3 origin = dropPoint != null ? dropPoint.position : transform.position;
        origin.y += 0.5f;

        foreach (DropInfo info in drops)
        {
            if (info == null || info.itemPrefab == null)
                continue;

            for (int i = 0; i < info.count; i++)
            {
                if (info.chance < 1f && Random.value > info.chance)
                    continue;

                Vector3 offset = new Vector3(Random.Range(-spreadX, spreadX), Random.Range(0f, spreadY), 0f);
                // Quái chết sát tường: kẹp điểm spawn để đồ không lọt sau tường
                Vector3 spawnPosition = ItemDropPhysics.ClampSpawnPosition(origin, origin + offset);
                Item droppedItem = Instantiate(info.itemPrefab, spawnPosition, Quaternion.identity);
                droppedItem.itemPrefab = info.itemPrefab;

                Rigidbody2D rb = droppedItem.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    ItemDropPhysics.PrepareRigidbody(rb);
                    float direction = Random.value < 0.5f ? -1f : 1f;
                    Vector2 force = new Vector2(direction * Random.Range(0.5f, burstForce), upwardForce);
                    rb.AddForce(force, ForceMode2D.Impulse);
                }
            }
        }
    }
}
