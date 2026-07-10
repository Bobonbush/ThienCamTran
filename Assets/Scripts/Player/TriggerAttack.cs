using UnityEngine;

public class TriggerAttack : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    Animator anim;

    // Turn off in the animator event
    SpriteRenderer spriteRenderer;
    void Start()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Trigger()
    {
        anim.SetTrigger(AnimationStrings.attack);

    }
}
