using UnityEditor.AssetImporters;
using UnityEngine;

public class Bossu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Animator anim;

    private Damageable damageable;
    private BossEffect effect;
    private Rigidbody2D rb;
    private TouchingDirections touching;
    private bool Released = false;

    public float gravityScale = 3.0f;

    private Transform playerTransform;

    [SerializeField]
    private bool RelasedSoon = false;


    private Vector2 _walkDiretionVector = Vector2.left;


    public Vector2 walkDiretionVector
    {
        get { return _walkDiretionVector; }
        private set { _walkDiretionVector = value; }
    }


    public bool CanMove
    {
        get
        {
            return anim.GetBool(AnimationStrings.canMove);
        }
    }


    private void Awake()
    {

        anim = GetComponentInChildren<Animator>();
        damageable = GetComponent<Damageable>();
        rb = GetComponent<Rigidbody2D>();
        effect = GetComponentInChildren<BossEffect>();
        touching = GetComponent<TouchingDirections>();
    }

    private void Start()
    {
        playerTransform = CutSceneManager.Instance.playerTransform;
        if(RelasedSoon)
        {
            
            Release();
        }
    }


    void Flip(Vector3 dirX)
    {
        if(dirX.x > 0 && walkDiretionVector.x < 0 )
        {
            _walkDiretionVector *= -1;
            anim.SetTrigger(AnimationStrings.isSwapDir);
        }else if(dirX.x < 0 && walkDiretionVector.x > 0)
        {
            _walkDiretionVector *= -1;
            anim.SetTrigger(AnimationStrings.isSwapDir);
        }
    }

    void Chase()
    {
        if (!CanMove) return;

        Flip(playerTransform.position - transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        if (!Released || CutSceneManager.Instance.IsInCutScene) return;
        
        Chase();
    }

    private void FixedUpdate()
    {
        if (!Released) return;
    }

    public void OnHit(int damage, Vector2 hitKnockback)
    {
        effect.BloodEffect(hitKnockback.normalized);
        effect.Flash();

    }

    public void Release()
    {
        Released = true;
        anim.SetBool("Released", true);
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravityScale;
    }

    public bool isAlive()
    {
        return damageable.IsAlive;
    }

    public void AnimationFlip()
    {
        transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
        
    }
}
