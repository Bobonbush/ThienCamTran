using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{


    private void OnTriggerEnter2D(Collider2D collision)
    {
        // See if it can be hitted
        Damageable damageable = collision.GetComponent<Damageable>();

        if (damageable != null)
        {

            bool gotHit = damageable.HitTrap();

            if (gotHit)
            {
                Debug.Log(collision.name + " hit because trap");
            }
        }
    }
}
