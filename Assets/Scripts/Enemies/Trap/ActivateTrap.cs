using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ActivateTrap : MonoBehaviour
{

    [SerializeField] private bool Reusable = false;
    [SerializeField] private float moveDuration = 0.3f; 
    [SerializeField] private float moveDistance = 0.25f;
    [SerializeField] private float delayRestore = 3.0f;
    [SerializeField] private Vector3 moveDir = new Vector3(0, -1, 0);


    private BoxCollider2D box;

    [System.Serializable]
    public struct TrapData
    {
        public GameObject trapObject;
        public float activationDelay;
    }

    public enum ExecutionMode { Parallel, Sequential }

    public enum Type { oneTimeTrigger, multipleTimeTrigger, continuousTrigger};

    [SerializeField]
    private bool oneTimeTriggerSave = false;

    public enum TriggerType { stand, purify};

    public bool isActivating
    {
        get { return collisionCnt > 0; }
    }

    int collisionCnt = 0;




    [SerializeField] private List<TrapData> trapSequence;

    [SerializeField] private ExecutionMode mode = ExecutionMode.Parallel;

    [SerializeField] Type type = Type.oneTimeTrigger;

    [SerializeField] TriggerType triggerType = TriggerType.stand;

    private Coroutine movementRoutine;

    private Vector3 initialPosition;

    [SerializeField] Transform realTransform;
    private float invertDistance = 1.0f;

    private void Start()
    {
        initialPosition = realTransform.position;
        float fullMoveDistance = Vector3.Distance(initialPosition, initialPosition + (moveDistance * moveDir));
        invertDistance /= fullMoveDistance;
       
    }

    public void ActivateTraps()
    {
        if (mode == ExecutionMode.Parallel)
        {
            StartCoroutine(RunParallel());
        }
        else
        {
            StartCoroutine(RunSequential());
        }
    }

    public void DeActiveTraps()
    {
        if (mode == ExecutionMode.Parallel)
        {
            Restore();
        }
    }


    private IEnumerator RunParallel()
    {

        float longestDelay = 0.0f;
        foreach (TrapData data in trapSequence)
        {
            StartCoroutine(ExecuteTrapAfterDelay(data));
            if (data.activationDelay > longestDelay)
                longestDelay = data.activationDelay;
        }

        if (type == Type.oneTimeTrigger)
        {
            yield return new WaitForSeconds(longestDelay);
            if (oneTimeTriggerSave == false)
                Destroy(this.gameObject);
            else
                this.enabled = false;
               
        } else if(type == Type.multipleTimeTrigger)
        {
            yield return new WaitForSeconds(longestDelay + delayRestore);
            Restore();
        }

    }

    private IEnumerator ExecuteTrapAfterDelay(TrapData data)
    {
        yield return new WaitForSeconds(data.activationDelay);
        TriggerTrap(data.trapObject);
    }


    private IEnumerator RunSequential()
    {
        foreach (TrapData data in trapSequence)
        {
            yield return new WaitForSeconds(data.activationDelay);
            TriggerTrap(data.trapObject);
        }

        if (type == Type.oneTimeTrigger)
        {
            Destroy(this.gameObject);
        } else if(type == Type.multipleTimeTrigger)
        {
            yield return new WaitForSeconds(delayRestore);
            Restore();
        }
    }

    private void TriggerTrap(GameObject trap)
    {
        if (trap != null)
        {
            trap.SetActive(true);
            trap.GetComponent<TrapMove>().ActivateTrap();
            
        }
    }


    public void TriggerAnimation()
    {
        if (movementRoutine != null) StopCoroutine(movementRoutine);

        Vector3 targetPos = initialPosition + (moveDistance * moveDir);

        float duration = moveDuration;
        if (type == Type.continuousTrigger)
        {
            duration = Vector3.Distance(initialPosition, transform.position) * invertDistance * moveDuration;
            duration = moveDuration - duration;
        }

        movementRoutine = StartCoroutine(SmoothMove(targetPos, duration));
    }

    public void Restore()
    {
        
        if (movementRoutine != null && type != Type.continuousTrigger) StopCoroutine(movementRoutine);

        movementRoutine = StartCoroutine(RestoreWhenClear(initialPosition));
    }

    private IEnumerator RestoreWhenClear(Vector3 targetPosition)
    {
        while (collisionCnt > 0 && triggerType != TriggerType.purify)
        {
            yield return null;
        }

        foreach (TrapData data in trapSequence)
        {
            TrapMove trap = data.trapObject.GetComponent<TrapMove>();
            if(trap != null)
            {
                if(trap.ReliedOnActivator)
                {
                    trap.StopTrap();
                }
            }
        }

        float duration = moveDuration;
        if (type == Type.continuousTrigger)
        {
            duration = Vector3.Distance(initialPosition, transform.position) * invertDistance * moveDuration;
            duration = moveDuration - duration;
        }

        yield return StartCoroutine(SmoothMove(targetPosition, duration));

        
        
    }

    


    private IEnumerator SmoothMove(Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = realTransform.position;
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            float t = timeElapsed / duration;


            t = Mathf.SmoothStep(0f, 1f, t);


            realTransform.position = Vector3.Lerp(startPosition, targetPosition, t);

            timeElapsed += Time.deltaTime;
            yield return null;
        }


        realTransform.position = targetPosition;
        movementRoutine = null;
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggerType == TriggerType.purify) return;
        collisionCnt++;
        if (collisionCnt == 1)
        {
            TriggerAnimation();
            ActivateTraps();
        }

    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (triggerType == TriggerType.purify) return;

        collisionCnt--;
        if(collisionCnt == 0 && type == Type.continuousTrigger)
        {
            Restore();
        }
    }
}
