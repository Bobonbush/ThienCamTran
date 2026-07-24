using TMPro;
using UnityEngine;

public class BossTrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    [SerializeField]
    private Bossu boss;
    
    public float duration = 2.0f;

    bool Triggered = false;
    private ActivateTrap activeTrap; // use for close the door

    private BoxCollider2D box;
    bool Released = false;
    private void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        activeTrap = GetComponent<ActivateTrap>();
    }
    // Update is called once per frame
    void Update()
    {
        if (Triggered == false) return;

        if(duration > 0)
        {
            duration -= Time.deltaTime;
        }else if(Released == false)
        {
            boss.Release();
            Released = true;
        }

        if(isDone())
        {
            Done();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.GetComponent<PlayerStats>() != null)
        {
            Triggered = true;
            GetComponent<CutTrigger>().Trigger(collision.GetComponent<PlayerController>(), collision.GetComponentInChildren<PlayerCamera>());
            ActivateTrap(true);
            box.enabled = false;
        }
    }

    private void ActivateTrap(bool active)
    {
        if (activeTrap != null)
        {
            if (active)
                activeTrap.ActivateTraps();
            else
                activeTrap.DeActiveTraps();
        }
    }


    private void Done()
    {
        ActivateTrap(false);
        this.enabled = false;
    }
    bool isDone()
    {
        return !boss.isAlive();
    }

   
}
