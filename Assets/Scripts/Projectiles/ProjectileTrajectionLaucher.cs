using TMPro;
using UnityEngine;

public class ProjectileTrajectionLaucher : MonoBehaviour
{
    public Transform target;
    public LineRenderer lineRenderer;


    [SerializeField]
    public GameObject projectilePrefab;

    public float preferredSpeed = 15f; // Base speed when in normal range
    public int lineResolution = 30;
    public bool useLowArc = true;
    float shootCooldown = 0.0f;
    public float maxShootCooldown = 5.0f;

    public float launchAngleDegrees = 45f; // The angle to launch at (45° gives max range)

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>() ;
    }

    private void Update()
    {
        Vector3 lauchVelocity = CalculateAdaptiveVelocity();

        DrawTrajectory(lauchVelocity);

        if (shootCooldown <= maxShootCooldown)
        {
            shootCooldown += Time.deltaTime;
            return;
        }

        shootCooldown = 0.0f;

        FireProjectile(lauchVelocity);

    }

    Vector3 CalculateAdaptiveVelocity()
    {
        Vector3 rawDirection = target.transform.position - transform.position;
        float y = rawDirection.y;
        Vector3 directionXZ = new Vector3(rawDirection.x, 0, rawDirection.z);
        float x = directionXZ.magnitude;
        float g = Mathf.Abs(Physics.gravity.y);

        float v = preferredSpeed;
        float v2 = v * v;
        float v4 = v2 * v2;

        // The formula for the required angle to hit a target at a specific speed
        // The term under the square root determines if the target is reachable
        float rootTerm = v4 - g * (g * x * x + 2 * y * v2);

        float finalAngleRad = 0f;
        bool needsSpeedRecalculation = false;

        if (rootTerm < 0)
        {
            // TARGET IS OUT OF RANGE at the preferred speed.
            // Default to 45 degrees (optimal range angle) to lob it as best as possible.
            finalAngleRad = 45f * Mathf.Deg2Rad;
            needsSpeedRecalculation = true;
        }
        else
        {
            // TARGET IS REACHABLE. Calculate the required angle.
            float root = Mathf.Sqrt(rootTerm);

            // Pick the high arc (+) or low arc (-)
            float numerator = (v2 - root);
            finalAngleRad = Mathf.Atan(numerator / (g * x));

            float finalAngleDeg = finalAngleRad * Mathf.Rad2Deg;

            // Apply your 15 to 85 bounds constraint
            if (finalAngleDeg < 15f || finalAngleDeg > 85f)
            {
                finalAngleDeg = Mathf.Clamp(finalAngleDeg, 15f, 85f);
                finalAngleRad = finalAngleDeg * Mathf.Deg2Rad;
                needsSpeedRecalculation = true; // Angle changed, so the old preferred speed will miss
            }
        }

        float finalSpeed = preferredSpeed;

        // If the angle was clamped or out of range, we must override the speed to guarantee a hit
        if (needsSpeedRecalculation)
        {
            // Recycling your original math to find the precise speed for our new clamped angle
            float numericalDenominator = 2 * Mathf.Pow(Mathf.Cos(finalAngleRad), 2) * (x * Mathf.Tan(finalAngleRad) - y);

            if (numericalDenominator > 0.001f)
            {
                finalSpeed = Mathf.Sqrt((g * x * x) / numericalDenominator);
            }
            else
            {
                // Extreme edge case fallback if the target is directly above the clamped 85 degree angle
                finalSpeed = Mathf.Max(preferredSpeed, Mathf.Sqrt(2 * g * Mathf.Abs(y)) + x * 0.5f);
            }
        }

        // Assemble the final launch vector
        Vector3 groundDirection = directionXZ.normalized;
        Vector3 launchVector = (groundDirection * Mathf.Cos(finalAngleRad)) + (Vector3.up * Mathf.Sin(finalAngleRad));

        return launchVector * finalSpeed;
    }



    public void FireProjectile(Vector3 lauchVelocity)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, projectilePrefab.transform.rotation);

        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        rb.linearVelocity = lauchVelocity;
    }

    void DrawTrajectory(Vector3 velocity)
    {
        lineRenderer.positionCount = lineResolution;
        Vector3 startPosition = transform.position;
        float g = Physics.gravity.y;

        // Calculate exact total time of flight based on vertical velocity component
        float vY = velocity.y;
        float yDiff = target.position.y - transform.position.y;

        float insideRoot = (vY * vY) - 2 * g * yDiff;
        float totalTime = 0f;

        if (insideRoot >= 0)
        {
            totalTime = (vY + Mathf.Sqrt(insideRoot)) / Mathf.Abs(g);
        }
        else
        {
            totalTime = 2f; // Fail-safe fallback time
        }

        for (int i = 0; i < lineResolution; i++)
        {
            float simulationTime = (i / (float)(lineResolution - 1)) * totalTime;

            // s = s0 + v0*t + 0.5*g*t^2
            Vector3 displacement = velocity * simulationTime + Vector3.up * 0.5f * g * simulationTime * simulationTime;
            lineRenderer.SetPosition(i, startPosition + displacement);
        }
    }
}
