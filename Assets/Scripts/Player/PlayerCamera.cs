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

    void Start()
    {
        
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
        
       
    }
}
