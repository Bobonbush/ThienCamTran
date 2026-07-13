using UnityEngine;
using System.Collections.Generic;

public class Sealed : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    [SerializeField]
    private List<SealedPuzzle> puzzles = new List<SealedPuzzle>();



    public bool isSealed()
    {
        int cnt = 0;
        for (int i = 0; i < puzzles.Count; i++)
        {
            cnt += (puzzles[i].Solved ? 0 : 1);
        }

        return cnt > 0;
    }
}
