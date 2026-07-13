using System.Collections;
using UnityEngine;
using UnityEngine.Splines;
public class EnemyMove : MonoBehaviour
{

    public float walkAcceleration = 50f;
    public float maxSpeed = 3f;
    public float walkStopRate = 0.1f;
    public DetectionGroundZone cliffDetectionZone;

   
    private ParticleSystem bloodPrefab;


    public bool stationalEnemy = false;


    public Vector3 spawn = Vector3.zero;

    public Vector3 offsetBlood = Vector3.zero;

    public bool moveInRange = false;
    public BoxCollider2D moveRange;

    Rigidbody2D rb;
    TouchingDirections touchingDirections;
    Animator animator;
    Damageable damageable;
    Bounds moveRangeBounds;
    bool hasMoveRangeBounds;


    public float FlipWaitingMaxTime = 2.0f;

    private Coroutine flipState = null;

    public enum WalkableDirection
    {
        Right,
        Left
    }

    [SerializeField] private Material flashMaterial;

    [SerializeField] private float duration;

    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;

    private Coroutine flashRoute;

    private WalkableDirection _walkDirection;


    [SerializeField]
    private Vector2 _walkDiretionVector = Vector2.right;


    public Vector2 walkDiretionVector
    {
        get {  return _walkDiretionVector; }
        private set { _walkDiretionVector = value;}
    }
    

    private Vector3 desiredPosition = Vector3.zero;
    private bool desiredMove = false;

    public bool lockedMove = false;

    public bool shouldMove = true;

    private float waitforNextFlip = 0.0f;
    private float maxWaitforNextFlip = 1.0f;




    private float initScaleX = 1f;

    public bool goingBack = false;

    public WalkableDirection WalkDirection
    {
        get { return _walkDirection; }
        set
        {
            if (_walkDirection != value)
            {
                gameObject.transform.localScale = new Vector2(gameObject.transform.localScale.x * -1, gameObject.transform.localScale.y);

                if (value == WalkableDirection.Right)
                {
                    walkDiretionVector = Vector2.right;
                }
                else if (value == WalkableDirection.Left)
                {
                    walkDiretionVector = Vector2.left;
                }
            }
            _walkDirection = value;
        }
    }
    


    public bool CanMove
    {
        get
        {
            return animator.GetBool(AnimationStrings.canMove);
        }
    }

    public float MoveImplitude
    {
        get
        {
            return animator.GetFloat(AnimationStrings.MoveImplitude);
        }
        private set
        {
            animator.SetFloat(AnimationStrings.MoveImplitude, value);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        touchingDirections = GetComponent<TouchingDirections>();
        animator = GetComponent<Animator>();
        damageable = GetComponent<Damageable>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        bloodPrefab = Resources.Load<ParticleSystem>("Effect/Blood");

        spawn = transform.position;

        initScaleX = transform.lossyScale.x;

        if (initScaleX < 0f)
        {
            _walkDirection = WalkableDirection.Left;
            WalkDirection = WalkableDirection.Left;
            walkDiretionVector = Vector2.left;

        }
        else
        {
            _walkDirection = WalkableDirection.Right;
            WalkDirection = WalkableDirection.Right;
            walkDiretionVector = Vector2.right;
        }


        cliffDetectionZone = GetComponentInChildren<DetectionGroundZone>();

        originalMaterial = spriteRenderer.material;

        if (moveRange != null)
        {
            moveRangeBounds = moveRange.bounds;
            hasMoveRangeBounds = true;
        }
    }

    private void Start()
    {
        spawn = transform.position;

        initScaleX = transform.transform.lossyScale.x;

        if (initScaleX < 0f)
        {
            _walkDirection = WalkableDirection.Left;
            WalkDirection = WalkableDirection.Left;
            walkDiretionVector = Vector2.left;
           
        }
        else
        {
            _walkDirection = WalkableDirection.Right;
            WalkDirection = WalkableDirection.Right;
            walkDiretionVector = Vector2.right;
        }
    }

    private void FixedUpdate()
    {
        MoveImplitude = Vector2.SqrMagnitude(rb.linearVelocity);

        if (flipState == null)
        {
            waitforNextFlip += Time.fixedDeltaTime;
        }

        if (desiredMove)
        {
            PerformDesiredMove();
            return;
        }


        if (lockedMove) return;


        if(stationalEnemy)
        {
            return;
        }

        if (cliffDetectionZone.detectedColliders.Count == 0)
        {
            rb.linearVelocityX = 0.0f;
        }


        if (touchingDirections.IsGrounded && (touchingDirections.IsOnWall || cliffDetectionZone.detectedColliders.Count == 0) && flipState == null && waitforNextFlip > maxWaitforNextFlip )
        {
            flipState = StartCoroutine(FlipDirection());
        }


        if (moveInRange && hasMoveRangeBounds && IsNextStepOutsideMoveRange() && flipState == null && waitforNextFlip > maxWaitforNextFlip)
        {
            KeepInsideMoveRange();
            //flipState = StartCoroutine(FlipDirection());
        }

        if (!damageable.LockVelocity && flipState == null )
        {
            Linger();
        }
    }


    private void PerformDesiredMove()
    {
        float horizontalDistance = desiredPosition.x - transform.position.x;

        if (Mathf.Abs(horizontalDistance) < 0.1f)
        {
            desiredMove = false; // Reached target
            rb.linearVelocity = new Vector2(Mathf.Lerp(rb.linearVelocity.x, 0, walkStopRate), rb.linearVelocity.y);

            if (goingBack)
            {
                goingBack = false;
                transform.localScale = new Vector3(initScaleX, transform.localScale.y, transform.localScale.z);

                if (initScaleX < 0f)
                {
                    walkDiretionVector = Vector2.left;
                    WalkDirection = WalkableDirection.Left;
                }
                else
                {
                    walkDiretionVector = Vector2.right;
                    WalkDirection = WalkableDirection.Right;
                }
            }
            return;
        }


        float directionX = Mathf.Sign(horizontalDistance);


        if (directionX > 0 && WalkDirection != WalkableDirection.Right)
        {
            WalkDirection = WalkableDirection.Right;
        }
        else if (directionX < 0 && WalkDirection != WalkableDirection.Left)
        {
            WalkDirection = WalkableDirection.Left;
        }

        float targetHorizontalVelocity = rb.linearVelocity.x + (directionX * walkAcceleration * Time.fixedDeltaTime);

        targetHorizontalVelocity = Mathf.Clamp(targetHorizontalVelocity, -maxSpeed, maxSpeed);

        rb.linearVelocity = new Vector2(targetHorizontalVelocity, rb.linearVelocity.y);
    }

    public void SetDesireMove(Vector3 position)
    {
        desiredMove = true;
        desiredPosition = position;

    }

    public void DenyDesireMove()
    {
        desiredMove = false;
    }

    private IEnumerator FlipDirection()
    {

        rb.linearVelocityX = 0.0f;
        yield return new WaitForSeconds(FlipWaitingMaxTime);

        if (WalkDirection == WalkableDirection.Right)
        {
            WalkDirection = WalkableDirection.Left;
        }
        else if (WalkDirection == WalkableDirection.Left)
        {
            WalkDirection = WalkableDirection.Right;
        }
        else
        {
            Debug.LogError("Invalid WalkDirection value: " + WalkDirection);
        }

        flipState = null;

        waitforNextFlip = 0.0f;
    }

    public void OnCliffDetected()
    {
        if (touchingDirections.IsGrounded)
        {
            flipState = StartCoroutine(FlipDirection());
        }
    }

    private bool IsNextStepOutsideMoveRange()
    {

        float currentX = transform.position.x;
        float nextX = currentX + walkDiretionVector.x * Mathf.Max(maxSpeed, Mathf.Abs(rb.linearVelocityX)) * Time.fixedDeltaTime;
        return currentX < moveRangeBounds.min.x || currentX > moveRangeBounds.max.x || nextX < moveRangeBounds.min.x || nextX > moveRangeBounds.max.x;
    }

    private void KeepInsideMoveRange()
    {
        Vector3 position = transform.position;

        desiredPosition.x = Mathf.Clamp(position.x, moveRangeBounds.min.x, moveRangeBounds.max.x);
        SetDesireMove(desiredPosition);
        desiredMove = true;
    }

    public void BloodEffect(Vector2 hit_direction)
    {
        

        Quaternion rotation = bloodPrefab.transform.rotation;

        if(hit_direction.x > 0)
        {
            rotation *= Quaternion.Euler(0, 180, 0);
        }
        Instantiate(
             bloodPrefab,
             transform.position + offsetBlood,
             rotation
        );
    }


    private void OnDrawGizmosSelected()
    {
        if (!moveInRange)
            return;

        Gizmos.color = Color.green;

        if (hasMoveRangeBounds)
            Gizmos.DrawWireCube(moveRangeBounds.center, moveRangeBounds.size);
        else if (moveRange != null)
            Gizmos.DrawWireCube(moveRange.bounds.center, moveRange.bounds.size);
    }


    private void Linger()
    {
        if (CanMove && touchingDirections.IsGrounded && !stationalEnemy && shouldMove)
        {

            rb.linearVelocity = new Vector2(
                Mathf.Clamp(rb.linearVelocity.x + (walkAcceleration * walkDiretionVector.x * Time.fixedDeltaTime), -maxSpeed, maxSpeed),
                rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(Mathf.Lerp(rb.linearVelocityX, 0, walkStopRate), rb.linearVelocity.y);
        }
    }

    public void Flash()
    {
        if(flashRoute != null)
        {
            StopCoroutine(flashRoute) ;
        }

        flashRoute = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        spriteRenderer.material = flashMaterial;

        yield return new WaitForSeconds(duration);

        spriteRenderer.material = originalMaterial;


        flashRoute = null;
    }
}
