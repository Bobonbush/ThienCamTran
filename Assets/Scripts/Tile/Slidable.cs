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

    public void Slide(Collider2D playerCollider)
    {
        StartCoroutine(PassThroughRoutine(playerCollider));
    }

    public void Slide(params Collider2D[] playerColliders)
    {
        StartCoroutine(PassThroughRoutine(playerColliders));
    }

    



    private IEnumerator PassThroughRoutine(Collider2D playerCollider)
    {
        Physics2D.IgnoreCollision(playerCollider, tilemapCollider, true);


        yield return new WaitForSeconds(0.5f);


        Physics2D.IgnoreCollision(playerCollider, tilemapCollider, false);
    }

    private IEnumerator PassThroughRoutine(Collider2D[] playerColliders)
    {
        // Turn off collision for every collider passed in
        foreach (var col in playerColliders)
        {
            Physics2D.IgnoreCollision(col, tilemapCollider, true);
        }

        yield return new WaitForSeconds(0.5f);

        foreach (var col in playerColliders)
        {

            if (col != null)
            {
                Physics2D.IgnoreCollision(col, tilemapCollider, false);
            }
        }
    }
}
