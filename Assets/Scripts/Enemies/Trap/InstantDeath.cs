using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
public class NewMonoBehaviourScript : MonoBehaviour
{


    private HashSet<Damageable> hitTargets = new HashSet<Damageable>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // See if it can be hitted
        Damageable damageable = collision.GetComponent<Damageable>();

        if (damageable != null)
        {

            
            bool gotHit = damageable.HitTrap();
            if(damageable.CheckingInvincible())
            {
                hitTargets.Add(damageable);
                damageable.OnInvincibleEnded += ResolveInvisible;
                Debug.Log("Found out the damagble in invisible hit trap");
            }
            if (gotHit)
            {
                Sfx.PlayAt(SfxId.WorldSpike, collision.bounds.center);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Damageable damageable = collision.GetComponent<Damageable>();

        if (damageable != null)
        {

            Debug.Log("It was Removed !");

            hitTargets.Remove(damageable);
            damageable.OnInvincibleEnded -= ResolveInvisible;

        }
    }

    private void ResolveInvisible(Damageable damageable)
    {
        Debug.Log("Called go go");
        if(hitTargets.Contains(damageable))
        {
            bool gotHit = damageable.HitTrap();
            
            if (gotHit)
            {
                damageable.OnInvincibleEnded -= ResolveInvisible;
                Sfx.PlayAt(SfxId.WorldSpike, damageable.transform.position);
                hitTargets.Remove(damageable);
            }
        }
    }
    
}
