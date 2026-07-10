using UnityEngine;

public class Purifying : MonoBehaviour, PuzzleEvent
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created



    public int Status { get; private set; } = 0;
    public float timing = 0.0f;

    


    public int GetStatus()
    {
        return Status;
    }

    public  void Trigger()
    {

    }

    public void ShowInteraction()
    {

    }


    public void OnTriggerEnter2D(Collider2D collision)
    {
        
    }
}
