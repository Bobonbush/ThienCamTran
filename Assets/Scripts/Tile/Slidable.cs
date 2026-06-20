using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
public class Sliable : MonoBehaviour
{
    private CompositeCollider2D tilemapCollider;

    void Start()
    {
        // This will grab the Composite Collider which handles the whole tilemap safely
        tilemapCollider = GetComponent<CompositeCollider2D>();
    }

    // Triggered by your Player's Down + Space input combo
    public void Slide(Collider2D playerCollider)
    {
        StartCoroutine(PassThroughRoutine(playerCollider));
    }

    private IEnumerator PassThroughRoutine(Collider2D playerCollider)
    {
        // Nothing
        Physics2D.IgnoreCollision(playerCollider, tilemapCollider, true);


        yield return new WaitForSeconds(0.5f);


        Physics2D.IgnoreCollision(playerCollider, tilemapCollider, false);
    }
}
