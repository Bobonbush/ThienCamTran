using UnityEngine;
using System.Collections;
public class MeleeEnemy : MonoBehaviour
{

    public float AlertMaxTime = 5.0f;


    
    public RayCast2D raycastInfo;

    private RaycastHit2D hit;
    private GameObject target;
    private bool InAtkRange = false;



    

    private bool Alert = false;

    private float AlertTiming = 10.0f;


    EnemyMove e_move;
    Rigidbody2D rb;
    Animator animator;

    public DetectionZone attackZone;

    [Header("Line of Sight")]
    [Tooltip("Layer chặn tầm nhìn (tường/đất). Để trống sẽ tự lấy layer Ground.")]
    public LayerMask sightBlockerMask;

    Collider2D bodyCollider;

    public bool _hasTarget = false;

    private float offsetDistance = 2.0f;
    public bool HasTarget
    {
        get
        {
            return _hasTarget;
        }
        private set
        {
            _hasTarget = value;
            animator.SetBool(AnimationStrings.hasTarget, value);
        }
    }


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        e_move = GetComponent<EnemyMove>();
        animator = GetComponent<Animator>();
        bodyCollider = GetComponent<Collider2D>();

        if (sightBlockerMask.value == 0)
            sightBlockerMask = LayerMask.GetMask("Ground");
    }


    private void Update()
    {
        HasTarget = GetVisibleTarget() != null;
    }

    private Collider2D GetVisibleTarget()
    {
        if (attackZone == null)
            return null;

        for (int i = attackZone.detectedColliders.Count - 1; i >= 0; i--)
        {
            Collider2D candidate = attackZone.detectedColliders[i];
            if (candidate == null)
            {
                attackZone.detectedColliders.RemoveAt(i);
                continue;
            }

            if (HasLineOfSight(candidate))
                return candidate;
        }

        return null;
    }

    // Không cho nhìn xuyên tường: linecast về phía nhân vật, vướng Ground là mất dấu
    private bool HasLineOfSight(Collider2D targetCollider)
    {
        Vector2 origin = bodyCollider != null ? (Vector2)bodyCollider.bounds.center : (Vector2)transform.position;

        if (Physics2D.Linecast(origin, targetCollider.bounds.center, sightBlockerMask).collider == null)
            return true;

        // Tâm bị che nhưng mép gần nhất có thể vẫn hở
        Vector2 closestPoint = targetCollider.bounds.ClosestPoint(origin);
        return Physics2D.Linecast(origin, closestPoint, sightBlockerMask).collider == null;
    }



    private void FixedUpdate()
    {
        Alert = false;
        if(AlertTiming <= AlertMaxTime)
        {
            Alert = true;
            AlertTiming += Time.fixedDeltaTime; 
        }
        
        if (InAtkRange)
        {

            hit = CastForTarget(e_move.walkDiretionVector);
            raycastInfo.RaycastDebugger(e_move.walkDiretionVector);

            if (hit.collider == null && Alert)
            {

                hit = CastForTarget(-e_move.walkDiretionVector);
                raycastInfo.RaycastDebugger(-e_move.walkDiretionVector);
            }
        }
        else
        {
            // Ra khỏi vùng là mất dấu, không giữ target cũ để đuổi xuyên tường
            hit = default;
        }

         

        if (hit.collider != null)
        {
            target = hit.collider.gameObject;
            ChaseLogic();
            e_move.lockedMove = true;
        } else
        {
            e_move.lockedMove = false;
        }
    }

    // Tia đuổi theo tính cả tường: chạm Ground trước khi chạm nhân vật -> coi như không thấy
    private RaycastHit2D CastForTarget(Vector2 direction)
    {
        RaycastHit2D rayHit = Physics2D.Raycast(
            raycastInfo.rayCast.position,
            direction,
            raycastInfo.rayCastLength,
            raycastInfo.raycastMask | sightBlockerMask);

        if (rayHit.collider != null && ((1 << rayHit.collider.gameObject.layer) & sightBlockerMask.value) != 0)
            return default;

        return rayHit;
    }

    private void ChaseLogic()
    {
        e_move.DenyDesireMove();
        Move();
    }


    


    private void Move()
    {
        if (e_move.CanMove)
        {
            
            Vector3 targetPosition = new Vector3(target.transform.position.x, transform.position.y, transform.position.z);
            float horizontalDistance = targetPosition.x - transform.position.x;
            Vector3 dir = targetPosition - transform.position;
            if (Mathf.Abs(horizontalDistance + offsetDistance) > Mathf.Abs(horizontalDistance - offsetDistance))
            {
                horizontalDistance = horizontalDistance - offsetDistance;
                dir.x -= offsetDistance;
            } else
            {
                horizontalDistance = horizontalDistance + offsetDistance;
                dir.x += offsetDistance;
            }

            float directionX = Mathf.Sign(horizontalDistance);

            if (Mathf.Abs(dir.x) > 0.1f)
            {
                dir = Vector3.Normalize(dir);
                rb.linearVelocity = dir * e_move.maxSpeed;

                if (directionX > 0 && e_move.WalkDirection != EnemyMove.WalkableDirection.Right)
                {
                    e_move.WalkDirection = EnemyMove.WalkableDirection.Right;
                }
                else if (directionX < 0 && e_move.WalkDirection != EnemyMove.WalkableDirection.Left)
                {
                    e_move.WalkDirection = EnemyMove.WalkableDirection.Left;
                }
            } else
            {
                directionX = targetPosition.x - transform.position.x;
                directionX = Mathf.Sign(directionX);


                if (directionX > 0 && e_move.WalkDirection != EnemyMove.WalkableDirection.Right)
                {
                    e_move.WalkDirection = EnemyMove.WalkableDirection.Right;
                }
                else if (directionX < 0 && e_move.WalkDirection != EnemyMove.WalkableDirection.Left)
                {
                    e_move.WalkDirection = EnemyMove.WalkableDirection.Left;
                }

                rb.linearVelocity = Vector2.zero;
            }

            TriggerAlert();
        }
    }

    private void ComeBackLogic()
    {
        e_move.SetDesireMove(e_move.spawn);
        e_move.goingBack = true;
    }



    public void OnHit(int damage, Vector2 knockback)
    {
        rb.linearVelocity = new Vector2(knockback.x, rb.linearVelocity.y + knockback.y);

        Vector2 dirHit = Vector2.Normalize(rb.linearVelocity);

        e_move.BloodEffect(dirHit);
        

        TriggerAlert();
        e_move.Flash();
    }



    private void TriggerAlert()
    {
        AlertTiming = 0.0f;
    }
    


    private int collisionCount = 0;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.GetComponent<PlayerController>() != null)
        {
            collisionCount++;
            target = collision.gameObject;
            InAtkRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.GetComponent<PlayerController>() != null)
        {
            collisionCount--;
            if (collisionCount == 0)
            {
                target = null;
                InAtkRange = false;

                if(e_move.stationalEnemy)
                {
                    ComeBackLogic();
                }
            }
        }
    }



}
