using UnityEngine;

public class SaveGround : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        

        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            Vector3 targetPosition = transform.position;
            player.SetSafeGround(targetPosition);
        }
    }
}
