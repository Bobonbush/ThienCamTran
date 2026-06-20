using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections;
public class Breakable : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    Damageable dm;
    Animator animator;
    void Start()
    {
        animator = GetComponentInParent<Animator>();
        dm = GetComponentInParent<Damageable>();
        dm.Health = 10;  // 3 hits
        
    }

    public void OnHit(int damage, Vector2 knockback)
    {

        animator.SetTrigger("hit"); // if have.
        dm.invicibilityTimer = -1.0f;

        if(!dm.IsAlive)
        {
            StartCoroutine(DieRoutine());
        }

    }




    IEnumerator DieRoutine()
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        // Wait one frame so the trigger takes effect
        yield return null;

        state = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(state.length);

        Destroy(transform.parent.gameObject);
    }
}
