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

    public bool _hasTarget = false;
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

            hit = Physics2D.Raycast(raycastInfo.rayCast.position, e_move.walkDiretionVector, raycastInfo.rayCastLength, raycastInfo.raycastMask);
            raycastInfo.RaycastDebugger(e_move.walkDiretionVector);

            if (hit.collider == null && Alert)
            {
                
                hit = Physics2D.Raycast(raycastInfo.rayCast.position, -e_move.walkDiretionVector, raycastInfo.rayCastLength, raycastInfo.raycastMask);
                raycastInfo.RaycastDebugger(e_move.walkDiretionVector);
            } 
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

            float directionX = Mathf.Sign(horizontalDistance);


            if (directionX > 0 && e_move.WalkDirection != EnemyMove.WalkableDirection.Right)
            {
                e_move.WalkDirection = EnemyMove.WalkableDirection.Right;
            }
            else if (directionX < 0 && e_move.WalkDirection != EnemyMove.WalkableDirection.Left)
            {
                e_move.WalkDirection = EnemyMove.WalkableDirection.Left;
            }

            Vector3 dir = targetPosition - transform.position;
            dir = Vector3.Normalize(dir);
            rb.linearVelocity = dir * e_move.maxSpeed;

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
