using System;
using UnityEngine;

[Serializable]
public struct RayCast2D
{
    public Transform rayCast;
    public LayerMask raycastMask;
    public float rayCastLength;

    public void RaycastDebugger(Vector2 direction)
    {
        Debug.DrawRay(rayCast.position, direction * rayCastLength, Color.red);
    }
}
