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
    
    
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(Closed) // Use for only from other to this.
        {
            return;
        }
        if(collision.GetComponent<PlayerController>())
        {
            SceneTransitionManager.Instance.TransitionToScene(LinkedRoom, SpawnPointId, spawnOffset);
        }
    }
}
