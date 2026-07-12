using Unity.Cinemachine;
using UnityEngine;
using System.Collections;
public class PlayerCamera : MonoBehaviour
{
    [Header("Target Setup")]
    [SerializeField] private Transform cameraTarget; // Drag 'CameraFollowTarget' here
    [SerializeField] private PlayerController player;

    [SerializeField] private CinemachineConfiner2D confiner;
    [SerializeField] private Collider2D globalBoundary; // Assign your world map boundary here

    [Header("Look Settings")]
    [SerializeField] private float lookDistance = 4f;       // How far the camera pans
    [SerializeField] private float activationDelay = 0.15f;   // How long you must hold the button
    [SerializeField] private float shiftSpeed = 8f;         // How fast the target moves

    [SerializeField] private float offsetTransitionSpeed = 3f;

    private Vector3 currentOffsetLocalPosition = Vector3.zero;




    private float timer = 0f;
    private Vector3 defaultLocalPosition;
    private Vector3 targetLocalPosition;

    private Vector3 offsetLocalPosition = Vector3.zero; // Use for custom camera Position; 

    public bool CutSceneLock = false;

    private Collider2D saveLocalBounds = null; 

    void Start()
    {
        if(globalBoundary == null)
        {
            UpdateGlobalCameraBoundary();
        }
        if (cameraTarget != null)
        {
            defaultLocalPosition = cameraTarget.localPosition;
            targetLocalPosition = defaultLocalPosition;
            
        }
    }

    void Update()
    {
        if(CutSceneLock)
        {
            return;
        }
        currentOffsetLocalPosition = Vector3.Lerp(
             currentOffsetLocalPosition,
             offsetLocalPosition,
             offsetTransitionSpeed * Time.deltaTime
        );

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
                targetLocalPosition = defaultLocalPosition + new Vector3(0, lookDistance, 0) + currentOffsetLocalPosition;
            } 
        }
        else if (player.IsIdle && verticalInput < -0.1f) // Holding DOWN
        {
            timer += Time.deltaTime;
            if (timer >= activationDelay)
            {
                targetLocalPosition = defaultLocalPosition + new Vector3(0, -lookDistance, 0) + currentOffsetLocalPosition;
            }
        }
        else // Moving or not pressing anything: instantly reset
        {
            
            timer = 0f;
            targetLocalPosition = defaultLocalPosition + currentOffsetLocalPosition;
        }

        // 3. Smoothly slide the target to its destination
        cameraTarget.localPosition = Vector3.MoveTowards(
            cameraTarget.localPosition,
            targetLocalPosition,
            shiftSpeed * Time.deltaTime
        );
    }

    public void SetCameraOffsetPosition(Vector3 offset)
    {
        offsetLocalPosition = offset;
    }

    public void UpdateGlobalCameraBoundary()
    {
        GameObject localBounds = GameObject.Find("CameraBounds");




        if (localBounds != null)
        {

            Collider2D targetCollider = localBounds.GetComponent<Collider2D>();


            confiner.BoundingShape2D = targetCollider;

            globalBoundary = targetCollider;
            saveLocalBounds = null;

            confiner.InvalidateBoundingShapeCache();
        }
    }


    public void UpdateLocalCameraBoundary(Collider2D localBounds)
    {
        
        confiner.BoundingShape2D = localBounds;

        saveLocalBounds = localBounds;
        confiner.InvalidateBoundingShapeCache();
    }

    private int collisionCount = 0;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("CameraCustomTrigger"))
        {
            return;
        }
        if (!CutSceneLock)
        {
            CameraCustomBound customSetting = collision.GetComponent<CameraCustomBound>();
            SetCameraOffsetPosition(customSetting.localPosition - defaultLocalPosition);

            if (customSetting.customBoundaries)
            {
                UpdateLocalCameraBoundary(customSetting.Boundaries);
            }
        }
        collisionCount++;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        
        if (collision.gameObject.layer != LayerMask.NameToLayer("CameraCustomTrigger"))
        {
            return;
        }

        if (--collisionCount == 0 && !CutSceneLock)
        {
            offsetLocalPosition = Vector3.zero;
            confiner.BoundingShape2D = globalBoundary;

            confiner.InvalidateBoundingShapeCache();
        }
    }

    public IEnumerator MoveCamera(Vector3 target, float duration)
    {
        CutSceneLock = true;

        Vector3 start = cameraTarget.position;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = timer / duration;

            cameraTarget.position = Vector3.Lerp(start, target, t);

            yield return null;
        }

        cameraTarget.position = target;
    }

    public IEnumerator Wait(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    public void LockCutScene()
    {
        CutSceneLock = true;
        
        UpdateLocalCameraBoundary(null);
    }

    public void ReleaseLockCutScene()
    {
        CutSceneLock = false;
        if(saveLocalBounds != null)
        {
            UpdateLocalCameraBoundary(saveLocalBounds);
        }
    }
}
