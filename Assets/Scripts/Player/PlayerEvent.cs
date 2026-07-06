using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerEvent : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private PuzzleEvent puzzleEvent = null;
    private PlayerController controller;
    
    void Start()
    {
        controller = gameObject.GetComponent<PlayerController>();    
    }

    // Update is called once per frame
    void Update()
    {

        if (puzzleEvent == null) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {

            puzzleEvent.Trigger();
            controller.lockInput = true;
        }

       

    }


    void ShowInteraction()
    {
        puzzleEvent.ShowInteraction();
    }


    private int collisionCnt = 0;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("HackingPuzzle"))
        {
            collisionCnt++;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
       if(controller.gameObject.CompareTag("HackingPuzzle"))
        {
            collisionCnt--;
        }
    }
}
