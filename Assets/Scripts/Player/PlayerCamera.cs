using System.Collections;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerCamera : MonoBehaviour
{
    [Header("Target Setup")]
    [SerializeField] private Transform cameraTarget; // Drag 'CameraFollowTarget' here
    [SerializeField] private PlayerController player;

    [SerializeField] private CinemachineCamera cmCamera;
    [SerializeField] private CinemachineConfiner2D confiner;
    [SerializeField] private Collider2D globalBoundary; // Assign your world map boundary here

    [Header("Look Settings")]
    [SerializeField] private float lookDistance = 4f;       // How far the camera pans
    [SerializeField] private float activationDelay = 0.15f;   // How long you must hold the button
    [SerializeField] private float shiftSpeed = 8f;         // How fast the target moves


    [Header("Fall Camera")]
    [SerializeField] private float fallVelocityThreshold = -12f;
    [SerializeField] private float fallOffset = -3f;
    [SerializeField] private float fallOffsetSpeed = 5f;
    [SerializeField] private float offsetTransitionSpeed = 3f;


    [Header("Default Hurt Shake")]
    [SerializeField] private float amplitude = 2f;
    [SerializeField] private float frequency = 8f;
    [SerializeField] private float duration = 0.15f;


    private float currentFallOffset = 0f;

    private Vector3 currentOffsetLocalPosition = Vector3.zero;


    private CinemachineImpulseSource impulseSource;

    private float timer = 0f;
    private Vector3 defaultLocalPosition;
    private Vector3 targetLocalPosition;

    private Vector3 offsetLocalPosition = Vector3.zero; // Use for custom camera Position; 

    public bool CutSceneLock = false;

    private Collider2D saveLocalBounds = null;


    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }
    void Start()
    {
        if (player == null)
        {
            player = GetComponentInParent<PlayerController>();
        }

        if (cameraTarget == null)
        {
            cameraTarget = transform;
        }

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
        if (player == null) return;

        // 1. Check if the player is standing still on the ground
        // (If your velocity is near 0, you are idle)

        // 2. Get vertical input (W/S or Arrow Keys)
        float verticalInput = player.lookInput.y;


        if (player.IsIdle && verticalInput > 0.1f) // Holding UP
        {
            timer += Time.deltaTime;
            if (timer >= activationDelay)
            {
                targetLocalPosition = defaultLocalPosition + new Vector3(0, lookDistance, 0) + currentOffsetLocalPosition + Vector3.up * currentFallOffset;
            } 
        }
        else if (player.IsIdle && verticalInput < -0.1f) // Holding DOWN
        {
            timer += Time.deltaTime;
            if (timer >= activationDelay)
            {
                targetLocalPosition = defaultLocalPosition + new Vector3(0, -lookDistance, 0) + currentOffsetLocalPosition + Vector3.up * currentFallOffset;
            }
        }
        else // Moving or not pressing anything: instantly reset
        {
            
            timer = 0f;
            targetLocalPosition = defaultLocalPosition + currentOffsetLocalPosition;
        }

        float desiredFallOffset = 0f;

        if (player.CurrentVelocity().y < fallVelocityThreshold)
        {
            desiredFallOffset = fallOffset;
        }

        currentFallOffset = Mathf.Lerp(
            currentFallOffset,
            desiredFallOffset,
            fallOffsetSpeed * Time.deltaTime
        );

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

            if (confiner == null || targetCollider == null)
            {
                return;
            }

            confiner.BoundingShape2D = targetCollider;

            globalBoundary = targetCollider;
            saveLocalBounds = null;

            confiner.InvalidateBoundingShapeCache();
        }
    }


    public void UpdateLocalCameraBoundary(Collider2D localBounds)
    {
        if (confiner == null)
        {
            return;
        }
        
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
            if (customSetting == null)
            {
                return;
            }

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
        if (cameraTarget == null)
        {
            yield break;
        }

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

    public void ZoomTo(float targetSize, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(ZoomRoutine(targetSize, duration));
    }

    private IEnumerator ZoomRoutine(float targetSize, float duration)
    {
        float startSize = cmCamera.Lens.OrthographicSize;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0, 1, t); // Ease in/out

            var lens = cmCamera.Lens;
            lens.OrthographicSize = Mathf.Lerp(startSize, targetSize, t);
            cmCamera.Lens = lens;

            yield return null;
        }

        var finalLens = cmCamera.Lens;
        finalLens.OrthographicSize = targetSize;
        cmCamera.Lens = finalLens;
    }


    public IEnumerator Earthquake(float duration)
    {
        float timer = 0;

        Vibrate(
             0.7f, // low-frequency motor
             1.0f, // high-frequency motor
             duration * 2.0f // duration
        );

        while (timer < duration)
        {
            if (impulseSource != null)
                impulseSource.GenerateImpulse(UnityEngine.Random.insideUnitCircle * 0.8f);

            timer += 0.15f;
            yield return new WaitForSeconds(0.15f);
        }

        
    }

    public void Shake()
    {
        // Camera trong một số scene (test) chưa gắn CinemachineImpulseSource
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
            Vibrate(
                0.7f, // low-frequency motor
                1.0f, // high-frequency motor
                0.3f // duration
                );
        }
    }

    public void Vibrate(float lowFrequency, float highFrequency, float duration)
    {
        if (Gamepad.current == null)
            return;

        StartCoroutine(VibrationRoutine(
            lowFrequency,
            highFrequency,
            duration
        ));
    }

    private IEnumerator VibrationRoutine(
        float lowFrequency,
        float highFrequency,
        float duration)
    {
        Gamepad.current.SetMotorSpeeds(
            lowFrequency,
            highFrequency
        );

        yield return new WaitForSeconds(duration);

        Gamepad.current.SetMotorSpeeds(0, 0);
    }
}
