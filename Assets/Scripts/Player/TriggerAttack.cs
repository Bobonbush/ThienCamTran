using UnityEngine;

public class TriggerAttack : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    Animator anim;

    // Turn off in the animator event
    SpriteRenderer spriteRenderer;

    bool successAttack = false;
    void Start()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Trigger()
    {
        
        successAttack = false;
        anim.SetTrigger(AnimationStrings.attack);

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!successAttack && collision.GetComponent<Damageable>())
        {
            Debug.Log("Sucess Attack");
            PlayerStats stats = GetComponentInParent<PlayerStats>();
            stats.Mana += 10;
        }
        successAttack = true;
    }


}
