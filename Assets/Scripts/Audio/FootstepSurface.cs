using UnityEngine;

/// <summary>
/// Marks a ground collider (tilemap or platform) with a material so footsteps
/// and landings pick the matching cue. Anything without this component sounds
/// like the default stone/concrete step.
/// </summary>
public class FootstepSurface : MonoBehaviour
{
    public enum Surface
    {
        Stone,
        Grass,
        Wood,
        Water,
    }

    public Surface surface = Surface.Stone;
}
