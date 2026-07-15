using UnityEngine;
using System.Collections;
public class CutTrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    enum TriggerType : int {
        Instant = 0,
        HitBox = 1,
        NeedTrigger = 2
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

    bool NoCutSceneRemain { get { return cnt == 0; } }

    float time = 0.0f;
    int cnt = 0;

    private bool isDone = false;

    private PlayerController playerController;
    private PlayerCamera playerCamera;


    bool MulthiThreadingLock = false;
    bool firstAnimatorTrigger = false;

    private IEnumerator AcquireLock()
    {
        while(MulthiThreadingLock)
        {
            yield return null;
        }
        MulthiThreadingLock = true;
    }

    private void ReleaseLock()
    {

        MulthiThreadingLock = false;
    }

    private IEnumerator ModifyCount(int x)
    {
        yield return AcquireLock();
        cnt += x;
        if(cnt > 0)
        {
            firstAnimatorTrigger = true;
        }
        ReleaseLock();
    }



    void Start()
    {
        if (type == TriggerType.Instant)
        {
            CutSceneManager manager = CutSceneManager.Instance;
            manager.OnCutSceneStart(info);
            startPlaying = true;
        }
    }

    public void Trigger(PlayerController player, PlayerCamera _camera )
    {
        if (type == TriggerType.NeedTrigger)
        {
            startPlaying = true;
            playerController = player;
            playerCamera = _camera;
            CutScenePerform();
        }
    }


    private IEnumerator PerformPlayerMove()
    {
        yield return ModifyCount(1);
        
        for(int i = 0; i < info.PlayerForceDatas.Count; i++)
        {
            CutSceneInfo.PlayerForceData data = info.PlayerForceDatas[i];
            yield return playerController.Wait(data.stayDuration);
        }

        yield return ModifyCount(-1);
    }

    private IEnumerator PerformCameraMove()
    {
        yield return ModifyCount(1);
        Vector3 savePositionCamera = playerCamera.transform.position;
        for(int i = 0; i< info.ForceCameraMovement.Count; i++)
        {
            CutSceneInfo.CameraForceData data = info.ForceCameraMovement[i];
            yield return playerCamera.Wait(data.delaybeforeMove);

            yield return playerCamera.MoveCamera(
                  data.position,
                  data.moveDuration);

            yield return playerCamera.Wait(data.stayDuration);

        }

        if(info.ComeBackCamera)
        {
            yield return playerCamera.MoveCamera(savePositionCamera, info.timeCameraComeBack);
        }

        yield return ModifyCount(-1);
    }


    private void CutScenePerform()
    {
        if(info.RealTimeAnimation)
        {
            playerCamera.LockCutScene();
            playerController.LockCutScene();

            StartCoroutine(PerformPlayerMove());

            StartCoroutine(PerformCameraMove());
        }
    }
    private void BigDialogAnimation()
    {
        if (time <= info.delayTime)
        {
            time += Time.deltaTime;
            return;
        }
        if (!played)
        {
            Transform target = bubbleTarget != null ? bubbleTarget : transform;
            CutSceneDialogManager.Instance.StartDialog(startNode, style, target);
            played = true;
        }

        if (!CutSceneDialogManager.Instance.AnimationDone())
        {
            return;
        }
        time += Time.deltaTime;
        if (time <= info.showTime + info.delayTime)
        {
            return;
        }

        CutSceneDialogManager.Instance.EndDialog();

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

        // Text only CutScene
        if(info.useBigDialog)
        {
            BigDialogAnimation();
            return;
        }

        if(NoCutSceneRemain && startPlaying && firstAnimatorTrigger)
        {
            isDone = true;
            AbandoneTrigger();
        }
    }




    private void AbandoneTrigger()
    {
        if(playerController != null)
        {
            playerController.ReleaseLockCutScene();
        }
        if(playerCamera != null)
        {
            playerCamera.ReleaseLockCutScene();
        }
        this.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(type == TriggerType.HitBox && startPlaying == false)
        {
            if (collision.GetComponent<CutHitBox>()) { 
                startPlaying = true;
            }
            return;
        }
    }
}
