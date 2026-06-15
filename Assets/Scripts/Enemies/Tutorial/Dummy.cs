using UnityEngine;

public class Dummy : MonoBehaviour
{

    Animator animator;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    

    public void OnHit(int damage, Vector2 knockback)
    {
        Debug.Log("Yeas");
        SpriteRenderer sp = GetComponent<SpriteRenderer>();
        Damageable dm = GetComponent<Damageable>();
        dm.Health = Mathf.Clamp(dm.Health, 50 , 100 ); // immortal
        
        animator.SetTrigger("hit");
        if (knockback.x < 0)
        {
            sp.flipX = false;
        }else
        {
            sp.flipX = true;
        }
        
    }
}
