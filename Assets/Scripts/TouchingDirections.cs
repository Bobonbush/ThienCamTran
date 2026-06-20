using UnityEngine;

public class TouchingDirections : MonoBehaviour
{
    public ContactFilter2D castFilter;

    public ContactFilter2D ladderFilter;

    public float groundDistance = 0.05f;
    public float wallDistnace = 0.2f;
    public float ceilingDistance = 0.05f;


    Collider2D[] overlaps = new Collider2D[10];
    
    CapsuleCollider2D touchingCol;
    Animator animator;

    RaycastHit2D[] groundHits = new RaycastHit2D[5];
    RaycastHit2D[] wallHits = new RaycastHit2D[5];
    RaycastHit2D[] ceilingHits = new RaycastHit2D[5];

    public Sliable sliable;

    [SerializeField]
    private bool _isGrounded = false;
    public bool IsGrounded
    {
        get
        {
            return _isGrounded;
        }

        private set
        {
            _isGrounded = value;
            animator.SetBool(AnimationStrings.isGrounded, value);
        }
    }
    [SerializeField]
    private bool _isOnWall = false;
    public bool IsOnWall
    {
        get
        {
            return _isOnWall;
        }

        private set
        {
            _isOnWall = value;
            animator.SetBool(AnimationStrings.isOnWall, value);
        }
    }
    [SerializeField]
    private bool _isOnCeiling = false;
    private Vector2 wallCheckDirection => gameObject.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
    public bool IsOnCeiling
    {
        get
        {
            return _isOnCeiling;
        }

        private set
        {
            _isOnCeiling = value;
            animator.SetBool(AnimationStrings.isOnCeiling, value);
        }
    }


    private bool _IsOnSlidable = false;

    private bool _canClimb = false;

    public bool canClimb
    {
        private set { _canClimb = value; }
        get { return _canClimb; }
    }

    public bool IsOnSlidable
    {
        get
        {
            return _IsOnSlidable;
        }

        set
        {
            _IsOnSlidable = value;
        }
    }



    private int slidableLayer;
    private void Awake()
    {
        touchingCol = GetComponent<CapsuleCollider2D>();
        animator = GetComponent<Animator>();

        slidableLayer = LayerMask.NameToLayer("Slidable");
        
    }



    // Update is called once per frame
    void FixedUpdate()
    {

        int count = touchingCol.Overlap(ladderFilter, overlaps);
        if(count > 0)
        {
            canClimb = true;
        }else
        {
            canClimb = false;
        }

        int groundHitCount = touchingCol.Cast(Vector2.down, castFilter, groundHits, groundDistance);
        IsGrounded = groundHitCount > 0;

        IsOnSlidable = false;


        for (int i = 0; i < groundHitCount; i++)
        {
            if (groundHits[i].collider.gameObject.layer == slidableLayer)
            {
                IsOnSlidable = true;
                sliable = groundHits[i].collider.GetComponent<Sliable>();
                break;
            }
        }

        int OnWallHitCount = touchingCol.Cast(wallCheckDirection, castFilter, wallHits, wallDistnace);

        int minusSlidable = 0;
        for (int i = 0; i < OnWallHitCount; i++)
        {
            if (wallHits[i].collider.gameObject.layer == slidableLayer)
            {
                minusSlidable++;
            }
        }
        IsOnWall = (OnWallHitCount - minusSlidable) > 0;

        int CeilingHitCount = touchingCol.Cast(Vector2.up, castFilter, ceilingHits, ceilingDistance);
        
        minusSlidable = 0;

        for (int i = 0; i < CeilingHitCount; i++)
        {
            if (ceilingHits[i].collider.gameObject.layer == slidableLayer)
            {
                minusSlidable++;
            }
        }


        IsOnCeiling = (CeilingHitCount - minusSlidable) > 0;
    }


}
