using UnityEngine;

public class Room : MonoBehaviour
{
    [SerializeField]
    private string RoomID = "OutSkirt";
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField]
    private string LinkedRoom = "OutSkirt-1";

    [SerializeField]
    private string SpawnPointId = "Outskirt-1-Top";

    [SerializeField]
    // Add offset to move out of range.
    private Vector2 spawnOffset = new Vector2(0, 0);
    
    
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.GetComponent<PlayerController>())
        {
            SceneTransitionManager.Instance.TransitionToScene(LinkedRoom, SpawnPointId, spawnOffset);
        }
    }
}
