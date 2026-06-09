using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Target Setup")]
    [SerializeField] private Transform cameraTarget; // Drag 'CameraFollowTarget' here
    [SerializeField] private PlayerController player;

    [Header("Look Settings")]
    [SerializeField] private float lookDistance = 4f;       // How far the camera pans
    [SerializeField] private float activationDelay = 0.15f;   // How long you must hold the button
    [SerializeField] private float shiftSpeed = 8f;         // How fast the target moves

    

    private float timer = 0f;
    private Vector3 defaultLocalPosition;
    private Vector3 targetLocalPosition;

    private float minSceneX { get; set; }
    private float maxSceneX { get; set; }
    private float minSceneY { get; set; }
    private float maxSceneY { get; set; }
    void Start()
    {
        minSceneX = -3.0f;
        maxSceneX = 100000000.0f;
        minSceneY = -100.0f;
        maxSceneY = 100000000.0f; 
        if (cameraTarget != null)
        {
            defaultLocalPosition = cameraTarget.localPosition;
            targetLocalPosition = defaultLocalPosition;
        }
    }

    void Update()
    {

        
        if (cameraTarget == null) return;

        // 1. Check if the player is standing still on the ground
        // (If your velocity is near 0, you are idle)

        // 2. Get vertical input (W/S or Arrow Keys)
        float verticalInput = player.lookInput.y;


        if (player.IsIdle && verticalInput > 0.1f) // Holding UP
        {
            timer += Time.deltaTime;
            if (timer >= activationDelay)
            {
                targetLocalPosition = defaultLocalPosition + new Vector3(0, lookDistance, 0);
            }
        }
        else if (player.IsIdle && verticalInput < -0.1f) // Holding DOWN
        {
            timer += Time.deltaTime;
            if (timer >= activationDelay)
            {
                targetLocalPosition = defaultLocalPosition + new Vector3(0, -lookDistance, 0);
            }
        }
        else // Moving or not pressing anything: instantly reset
        {
            timer = 0f;
            targetLocalPosition = defaultLocalPosition;
        }

        // 3. Smoothly slide the target to its destination
        cameraTarget.localPosition = Vector3.MoveTowards(
            cameraTarget.localPosition,
            targetLocalPosition,
            shiftSpeed * Time.deltaTime
        );

        //Debug.Log("Camera Pos : " + cameraTarget.localPosition.x);
        Vector3 boundedWorldPosition = cameraTarget.position;

        boundedWorldPosition.x = Mathf.Clamp(boundedWorldPosition.x, minSceneX, maxSceneX);
        boundedWorldPosition.y = Mathf.Clamp(boundedWorldPosition.y, minSceneY, maxSceneY);
        // Apply the clamped position back to the world position
        cameraTarget.position = boundedWorldPosition;

        
       
    }

    // Trigger to adjust the camera max and camera min, not always follow the player.
    public void LoadScene(float minX, float maxX )
    {
        minSceneX = minX;
        maxSceneX = maxX;
    }
}
