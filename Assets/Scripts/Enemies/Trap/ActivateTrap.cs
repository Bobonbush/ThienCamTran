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
    [SerializeField] private bool UseLocalPosition = false;


    [SerializeField] private bool ActivateOnStart = false;
    [SerializeField] private bool needRestoreTrap = true;

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

    public bool isoneTimeTriggerSave { get { return oneTimeTriggerSave; } }

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

    private void Awake()
    {
        initialPosition = realTransform.position;
        if(UseLocalPosition) {
            initialPosition = realTransform.localPosition;
        }
        float fullMoveDistance = Vector3.Distance(initialPosition, initialPosition + (moveDistance * moveDir));
        invertDistance /= fullMoveDistance;
    }

    private void Start()
    {
        if(ActivateOnStart)
        {
            TriggerAnimation();
            ActivateTraps();
        }
    }


    public void InstantActivateTraps()
    {
        StartCoroutine(RunInstant());   
    }

    private IEnumerator RunInstant()
    {
        float longestDelay = 0.0f;
        foreach (TrapData data in trapSequence)
        {
            TriggerTrap(data.trapObject);
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

        }
        else if (type == Type.multipleTimeTrigger)
        {
            yield return new WaitForSeconds(longestDelay + delayRestore);
            Restore();
        }
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

        Sfx.PlayAt(SfxId.WorldPlatePress, realTransform.position);

        Vector3 targetPos = initialPosition + (moveDistance * moveDir);

        float duration = moveDuration;
        if (type == Type.continuousTrigger)
        {
            duration = Vector3.Distance(initialPosition, transform.position) * invertDistance * moveDuration;
            if(UseLocalPosition)
            {
                duration = Vector3.Distance(initialPosition, transform.localPosition) * invertDistance * moveDuration;
            }
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

        if (needRestoreTrap)
        {
            foreach (TrapData data in trapSequence)
            {
                TrapMove trap = data.trapObject.GetComponent<TrapMove>();
                if (trap != null)
                {
                    if (trap.ReliedOnActivator)
                    {
                        trap.StopTrap();
                    }
                }
            }
        }

        float duration = moveDuration;
        if (type == Type.continuousTrigger)
        {
            if (UseLocalPosition)
            {
                duration = Vector3.Distance(initialPosition, realTransform.localPosition);
            }
            else
            {
                duration = Vector3.Distance(initialPosition, realTransform.position);
            }

            duration *= invertDistance * moveDuration;

            duration = moveDuration - duration;
            duration = moveDuration - duration;
        }

        if (!isoneTimeTriggerSave) {
            Sfx.PlayAt(SfxId.WorldPlatePress, realTransform.position, 0.6f, 1.08f);
            yield return StartCoroutine(SmoothMove(targetPosition, duration));
        }
        
        
    }

    


    private IEnumerator SmoothMove(Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = realTransform.position;
        if (UseLocalPosition)
        {
            startPosition = realTransform.localPosition;
        }
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            float t = timeElapsed / duration;


            t = Mathf.SmoothStep(0f, 1f, t);



            
            if (UseLocalPosition)
            {
                realTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            }
            else
            {
                realTransform.position = Vector3.Lerp(startPosition, targetPosition, t);

            }
            timeElapsed += Time.deltaTime;
            yield return null;
        }


        if (UseLocalPosition)
        {
            realTransform.localPosition = targetPosition;
        }
        else
        {
            realTransform.position = targetPosition;
        }
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
