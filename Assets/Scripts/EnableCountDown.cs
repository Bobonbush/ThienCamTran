using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections;

public class EnableCountDown : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField]
    public float enableTiming = 1.0f;

    private void Start()
    {
        StartCoroutine(CountdownRoutine());
    }


    // Update is called once per frame
    IEnumerator CountdownRoutine() {
        yield return new WaitForSeconds(enableTiming);

        
        this.gameObject.SetActive(true);

        Destroy(this);
    }
}
