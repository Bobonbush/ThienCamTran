using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
    public Transform launchPoint;
    public GameObject projectilePrefab;
    public float projectileSpeedOverride = 0f;

    public Vector2 LaunchPosition
    {
        get { return launchPoint != null ? launchPoint.position : transform.position; }
    }


    public void FireProjectileForward()
    {
        GameObject projectile = Instantiate(projectilePrefab, LaunchPosition, projectilePrefab.transform.rotation);
        Vector3 origScale = projectile.transform.localScale;

        // Giữ độ lớn scale của prefab, chỉ lật hướng — bản cũ ghi đè thành ±1
        // nên prefab thu nhỏ sẽ bị méo lệch trục (giống FireProjectileInDirection)
        float direction = transform.localScale.x > 0 ? 1 : -1;
        projectile.transform.localScale = new Vector3(
            Mathf.Abs(origScale.x) * direction,
            origScale.y,
            origScale.z
            );
    }

    public void FireProjectileInDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            FireProjectileForward();
            return;
        }

        GameObject projectile = Instantiate(projectilePrefab, LaunchPosition, projectilePrefab.transform.rotation);
        direction.Normalize();

        float scaleX = direction.x >= 0 ? 1 : -1;
        Vector3 origScale = projectile.transform.localScale;
        projectile.transform.localScale = new Vector3(Mathf.Abs(origScale.x) * scaleX, origScale.y, origScale.z);

        Projectile simpleProjectile = projectile.GetComponent<Projectile>();
        if (simpleProjectile != null)
        {
            float speed = projectileSpeedOverride > 0 ? projectileSpeedOverride : simpleProjectile.moveSpeed.magnitude;
            simpleProjectile.Launch(direction * speed);
            return;
        }

        ProjectileTrajectory trajectoryProjectile = projectile.GetComponent<ProjectileTrajectory>();
        if (trajectoryProjectile != null)
        {
            float speed = projectileSpeedOverride > 0 ? projectileSpeedOverride : 12f;
            trajectoryProjectile.Launch(direction * speed);
            return;
        }

        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float speed = projectileSpeedOverride > 0 ? projectileSpeedOverride : 10f;
            rb.linearVelocity = direction * speed;
        }
    }
}
