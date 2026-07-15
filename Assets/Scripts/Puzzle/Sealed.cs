using UnityEngine;
using System.Collections.Generic;

public class Sealed : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    [SerializeField]
    private List<SealedPuzzle> puzzles = new List<SealedPuzzle>();


    private DialogInteractable interactable;

    private void Awake()
    {
        interactable = GetComponentInChildren<DialogInteractable>();
    }

    public bool isSealed()
    {
        for (int i = 0; i < puzzles.Count; i++)
        {

            if (puzzles[i].Solved == false)
            {
                return true;
            }
        }

        return false;
    }

    public void TriggerWarning()
    {
        if(interactable)
        {
            interactable.TriggerInteract();
        }
    }
}
