using UnityEngine;

/// <summary>
/// Chống đồ rơi (rương, quái) văng xuyên tường:
/// - Vị trí spawn ngẫu nhiên quanh nguồn có thể lọt sẵn sau tường khi nguồn
///   kê sát tường -> linecast từ nguồn tới điểm spawn, vướng thì kéo lùi lại.
/// - Đồ bị AddForce tới ~4 m/s dễ tunnel qua tilemap mỏng nếu rigidbody để
///   Discrete -> ép Continuous cho mọi vật phẩm được thả ra.
/// Dùng chung bởi ItemContainer.DropItems và EnemyDrops.DropAll.
/// </summary>
public static class ItemDropPhysics
{
    private static int blockerMask = -1;

    private static int BlockerMask
    {
        get
        {
            if (blockerMask < 0)
                blockerMask = LayerMask.GetMask("Ground", "Slidable");
            return blockerMask;
        }
    }

    public static Vector3 ClampSpawnPosition(Vector3 origin, Vector3 desired)
    {
        Vector2 delta = desired - origin;
        if (delta.sqrMagnitude < 0.0001f)
            return desired;

        RaycastHit2D hit = Physics2D.Linecast(origin, desired, BlockerMask);
        if (hit.collider == null)
            return desired;

        // Lùi khỏi mặt tường một chút để collider không kẹt trong tường
        return hit.point - delta.normalized * 0.15f;
    }

    public static void PrepareRigidbody(Rigidbody2D rb)
    {
        if (rb != null)
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }
}
