using UnityEngine;

public class CutTrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    enum TriggerType : int {
        Instant = 0,
        HitBox = 1
    }

    bool startPlaying = false;

    
    [SerializeField]
    CutSceneInfo info;

    [SerializeField]
    TriggerType type = TriggerType.Instant;

    public DialogNode startNode;                 // First node
    public DialogStyle style = DialogStyle.Box;  // Box or Bubble
    public Transform bubbleTarget;               // Who the bubble follows (empty = this object)

    
    bool played = false;

    float time = 0.0f;


    void Start()
    {
        if (type == TriggerType.Instant)
        {
            CutSceneManager manager = CutSceneManager.Instance;
            manager.OnCutSceneStart(info);
            startPlaying = true;
        }
    }


    private void DialogAnimation()
    {
        if (time <= info.delayTime)
        {
            time += Time.deltaTime;
            return;
        }
        if (!played)
        {
            Transform target = bubbleTarget != null ? bubbleTarget : transform;
            DialogManager.Instance.StartDialog(startNode, style, target);
            played = true;
        }

        if (!DialogManager.Instance.AnimationDone())
        {
            return;
        }
        time += Time.deltaTime;
        if (time <= info.showTime + info.delayTime)
        {
            return;
        }

        DialogManager.Instance.EndDialog();

        if (time <= info.fadingTime + info.delayTime + info.showTime)
        {
            return;
        }

        CutSceneManager manager = CutSceneManager.Instance;
        manager.OnCutSceneEnd(info);
        Destroy(this.gameObject);
    }

    private void Update()
    {
        if(!startPlaying)
        {
            return;
        }
        if(info.useDialog)
        {
            DialogAnimation();
        }
    }



    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (type == TriggerType.Instant) return;
        if(type == TriggerType.HitBox)
        {
            if (collision.GetComponent<CutHitBox>()) { 
                startPlaying = true;
            }
            return;
        }
    }
}
