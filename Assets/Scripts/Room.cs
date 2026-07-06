using UnityEngine;

public class Room : MonoBehaviour
{

    [SerializeField]
    private string LinkedRoom = "OutSkirt-1";

    [SerializeField]
    private string SpawnPointId = "Outskirt-1-Top";

    [SerializeField]
    // Add offset to move out of range.
    private Vector2 spawnOffset = new Vector2(0, 0);

    [SerializeField]

    public bool Closed = false;

    [SerializeField]

    public bool isDoor = false;

    private int collisionCnt = 0;

    public void EnterNextRoom()
    {
        SceneTransitionManager.Instance.TransitionToScene(LinkedRoom, SpawnPointId, spawnOffset);
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(Closed) // Use for only from other to this.
        {
            return;
        }
        
        if(collision.GetComponent<PlayerController>())
        {
            if (isDoor)
            {
                collisionCnt++;
                return;
            }
            EnterNextRoom();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if(isDoor && collision.GetComponent<PlayerController>())
        {
            collisionCnt--;
            return;
        }
    }
}
