using UnityEngine;

public class SaveZone : MonoBehaviour, IInteractable
{


    public int inUsed = 0;
    public bool CanInteract { get { return true; } }
    [SerializeField] Transform left;
    [SerializeField] Transform right;

    [Tooltip("Chặn spam E: khoảng nghỉ tối thiểu giữa hai lần vào/ra trạng thái pray")]
    [SerializeField] private float interactCooldown = 0.8f;
    private float lastToggleAt = -999f;


    public bool SaveGameMenu(PlayerController player)
    {
        return player.EnterSaving(left.position, right.position);
    }

    public bool ExitSaveGame(PlayerController player)
    {
        return player.ExitSaving();
    }

    public void Interact(PlayerController player)
    {

        if (!CanInteract || collisionCnt == 0)
        {
            return;
        }

        // Spam E khi đang pray sẽ lặp enter->exit->enter, mỗi vòng lại đánh
        // chuông + chạy lại animation — bắt buộc nghỉ giữa hai lần toggle
        if (Time.time < lastToggleAt + interactCooldown)
        {
            return;
        }

        if (inUsed == 0)
        {

            if (SaveGameMenu(player)) { inUsed ^= 1; lastToggleAt = Time.time; }
        }
        else
            if (ExitSaveGame(player)) { inUsed ^= 1; lastToggleAt = Time.time; }


    }

    public new IInteractable.Type GetType()
    {
        return IInteractable.Type.Save;
    }

    private int collisionCnt = 0;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.layer == LayerMask.NameToLayer("Player") )
        {
            collisionCnt++;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            collisionCnt--;
        }
    }



}
