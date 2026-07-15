using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DetectionZone : MonoBehaviour
{
    public UnityEvent noCollidersRemain;
    public List<Collider2D> detectedColliders = new List<Collider2D>();
    Collider2D col;
    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponentInParent<PlayerController>() != null && !detectedColliders.Contains(collision))
            detectedColliders.Add(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!detectedColliders.Remove(collision))
            return;

        if (detectedColliders.Count <= 0)
            noCollidersRemain.Invoke();
    }

}
