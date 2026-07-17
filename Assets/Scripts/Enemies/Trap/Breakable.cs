using UnityEngine;
using System.Collections;
public class Breakable : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    Damageable dm;
    Animator animator;


    [SerializeField]
    private GameObject attachedDeletation;

    void Start()
    {
        animator = GetComponentInParent<Animator>();
        dm = GetComponentInParent<Damageable>();
        dm.Health = 1;  // 1 hits

        // Đã vỡ trong save -> dọn luôn không animation/SFX khi vào lại scene
        if (SaveManager.Instance.IsObjectBroken(SaveIdUtility.For(this)))
        {
            if (attachedDeletation != null)
                Destroy(attachedDeletation.gameObject);
            Destroy(transform.parent.gameObject);
        }
    }

    public void OnHit(int damage, Vector2 knockback)
    {

        animator.SetTrigger("hit"); // if have.

        dm.invicibilityTimer = -1.0f;

        if(!dm.IsAlive)
        {
            SaveManager.Instance.MarkObjectBroken(SaveIdUtility.For(this));
            Sfx.PlayAt(SfxId.WorldBreak, transform.position);
            StartCoroutine(DieRoutine());
        }

    }




    IEnumerator DieRoutine()
    {
        AnimatorStateInfo state;

        state = animator.GetCurrentAnimatorStateInfo(0);

        // Wait one frame so the trigger takes effect


        yield return null;
        


        if (attachedDeletation != null)
        {
             HiddenPath hd = attachedDeletation.GetComponent<HiddenPath>();
             if (hd)
             {
                  hd.StartFade(0f);
             }
        }


        if(animator != null) { 
            state = animator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(state.length);
        }

        if (attachedDeletation != null) {
            Destroy(attachedDeletation.gameObject);
        }
        Destroy(transform.parent.gameObject);
    }
}
