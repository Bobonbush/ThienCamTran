using UnityEditor.SceneManagement;
using UnityEngine;

public class Breakable : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    Damageable dm;
    Animator animator;
    void Start()
    {
        animator = GetComponentInParent<Animator>();
        dm = GetComponentInParent<Damageable>();
        dm.Health = 30;  // 3 hits
        
    }

    public void OnHit(int damage, Vector2 knockback)
    {

        animator.SetTrigger("hit"); // if have.
        dm.invicibilityTimer = -1.0f;

        if(!dm.IsAlive)
        {
            
            Destroy(transform.parent.gameObject);
        }

    }
}
