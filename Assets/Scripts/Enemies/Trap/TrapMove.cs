using System.Collections;
using TMPro;
using UnityEngine;
using static ActivateTrap;

public class TrapMove : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField] private float moveDistance = 2.0f;
    [SerializeField] private float resetDelay = 3.0f; // X seconds to wait before resetting


    

    [SerializeField] private bool DestroyOnActivate = false;
    [SerializeField] private bool reset = true; // Turn this on to enable auto-resetting
    [SerializeField] public bool ReliedOnActivator = false;
    [SerializeField] private bool needActivator = true;
    [SerializeField] private bool useBothDeAndActive = true;

    [SerializeField] private bool turnOffColliderOnDeActive = true;

    [SerializeField] private float maxReActivateWaitingTime = 5.0f;
    private float ReActivateCountingTime = 6.0f;

    private int state = 0;      

    [SerializeField]
    private Vector3 moveDir = new Vector3(0.0f, -1.0f, 0.0f);
    
    private Vector3 initialPosition;
    private Coroutine movementRoutine;
    private Coroutine smoothMoveRoutine;

    private BoxCollider2D box;

    private float invertDistance = 1.0f;

    void Start()
    {
        initialPosition = transform.position;

        float fullMoveDistance = Vector3.Distance(initialPosition, initialPosition + (moveDistance * moveDir));
        invertDistance /= fullMoveDistance;

        box = GetComponent<BoxCollider2D>();

        box.enabled = false;

        if (useBothDeAndActive && needActivator == false) reset = false;
    }
    private void Update()
    {
        if (needActivator) return;
        
        if(movementRoutine != null)
        {
            return;
        }

        if(ReActivateCountingTime < maxReActivateWaitingTime)
        {
            ReActivateCountingTime += Time.deltaTime;
            return;
        }

        if(useBothDeAndActive)
        {
            if(state == 0 )
            {
                state ^= 1;
                ActivateTrap();
            } else
            {
                state ^= 1;
                StopTrap();
            }
        } else
        {
            ActivateTrap();
        }

        ReActivateCountingTime = 0.0f;

    }

    public void ActivateTrap()
    {
        if (movementRoutine != null)
        {
            StopCoroutine(movementRoutine);
        }

        if (smoothMoveRoutine != null)
        {
            StopCoroutine (smoothMoveRoutine);
        }
        Vector3 targetPos = initialPosition + (moveDir * moveDistance);

        box.enabled = true;
        float duration = moveDuration;
        if (ReliedOnActivator) {
            duration = Vector3.Distance(initialPosition, transform.position) * invertDistance * moveDuration;
            duration = moveDuration - duration;
        }
        movementRoutine = StartCoroutine(TrapSequence(targetPos, duration));
    }

    public void StopTrap()
    {
        if (movementRoutine != null) StopCoroutine(movementRoutine);

        Vector3 targetPos = initialPosition;
        float duration = moveDuration;
        if (ReliedOnActivator)
        {
            duration = Vector3.Distance(initialPosition, transform.position) * invertDistance * moveDuration;
        }

        movementRoutine = StartCoroutine(TrapSequence(targetPos, duration));

       
    }


    private IEnumerator TrapSequence(Vector3 targetPosition, float duration)
    {

        if (smoothMoveRoutine != null)
        {
            StopCoroutine(smoothMoveRoutine);
        }
        smoothMoveRoutine = StartCoroutine(SmoothMove(targetPosition, duration));
        yield return smoothMoveRoutine ;


        if (DestroyOnActivate)
        {
            Destroy(this.gameObject);
            yield break;
        }

        if (reset && !ReliedOnActivator)
        {

            yield return new WaitForSeconds(resetDelay);

            yield return StartCoroutine(SmoothMove(initialPosition, duration));
        }

        movementRoutine = null;
        if (initialPosition == transform.position && turnOffColliderOnDeActive == true)
        {
            box.enabled = false;
        }
    }




    private IEnumerator SmoothMove(Vector3 targetPosition, float duration)
    {
        // If there's no time or we are already there, snap and exit
        if (duration <= 0.001f || transform.position == targetPosition)
        {
            transform.position = targetPosition;
            yield break;
        }

        // Calculate a consistent physical speed (Units per second) based on your settings
        float speed = moveDistance / moveDuration;

        while (transform.position != targetPosition)
        {
            // Move towards the target at a perfectly steady speed, frame by frame
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }
    }

}
