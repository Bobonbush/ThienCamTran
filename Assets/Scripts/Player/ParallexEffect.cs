using UnityEngine;
using Unity.Cinemachine;


public class ParallaxEffect : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public Transform followTarget;

    // These variables will store the initial starting state
    private Vector2 startingPosition;
    private float startingZ;

    // 1. Calculates how far the camera has moved since the start
    Vector2 camMoveSinceStart => (Vector2)cam.transform.position - startingPosition;

    // 2. Calculates the Z distance between this object and the target
    // FIXED: Accessing the z component directly from position
    float zDistanceFromTarget => transform.position.z - followTarget.position.z;

    // 3. Determines the clipping plane based on whether the object is in front of or behind the target
    // FIXED: Ensured all parentheses match perfectly
    float clippingPlane => cam.transform.position.z + (zDistanceFromTarget > 0 ? cam.farClipPlane : cam.nearClipPlane);

    // 4. Calculates the final multiplier factor for the parallax movement
    float parallaxFactor => Mathf.Abs(zDistanceFromTarget) / clippingPlane;

    // Start is called before the first frame update
    void Start()
    {
        // Cache the starting position and Z depth of this object
        startingPosition = transform.position;
        startingZ = transform.position.z;

        // Auto-assign Main Camera if it was left unassigned in the Inspector
        if (cam == null)
        {
            cam = Camera.main;
        }

        CinemachineCamera virtualCam = FindFirstObjectByType<CinemachineCamera>();
        followTarget = virtualCam.Follow;
    }

    // Update is called once per frame
    void Update()
    {
        // When the target moves, move the parallax object the same distance times a multiplier
        Vector2 newPosition = startingPosition + camMoveSinceStart * parallaxFactor;

        // The X/Y position changes based on target movement times the parallax factor, but Z stays consistent
        transform.position = new Vector3(newPosition.x, newPosition.y, startingZ);
    }
}