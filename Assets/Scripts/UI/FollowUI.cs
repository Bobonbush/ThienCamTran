using UnityEngine;

public class FollowUI : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);

    private Camera cam;


    void Awake()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        Vector3 screenPos = target.position + offset;


        transform.position = screenPos; 
    }
}
