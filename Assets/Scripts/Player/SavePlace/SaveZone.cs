using UnityEngine;

public class SaveZone : MonoBehaviour, IInteractable
{


    public int inUsed = 0;
    public bool CanInteract { get { return true; } }
    [SerializeField] Transform left;
    [SerializeField] Transform right;


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

        if (inUsed == 0)
        {

            if(SaveGameMenu(player)) inUsed ^= 1;
        }
        else
            if(ExitSaveGame(player)) inUsed ^= 1 ;

        
    }

    public IInteractable.Type GetType()
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
