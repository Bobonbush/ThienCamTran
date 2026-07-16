using System;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "SfxLibrary", menuName = "Audio/Sfx Library")]
public class SfxLibrary : ScriptableObject
{
    [Serializable]
    public class Cue
    {
        public string id;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 1.5f)] public float minPitch = 1f;
        [Range(0.5f, 1.5f)] public float maxPitch = 1f;
        [Tooltip("Minimum seconds between two plays of this cue, to avoid machine-gun spam.")]
        public float cooldown = 0.04f;

        [Tooltip("Optional second clip layered on top of the main one for a thicker sound.")]
        public AudioClip[] layerClips;
        [Range(0f, 1f)] public float layerVolume = 0.5f;
    }

    [Tooltip("Mixer group every SFX voice is routed through, so the SfxVolume slider applies.")]
    public AudioMixerGroup output;

    [Header("Positional falloff (Sfx.PlayAt)")]
    [Tooltip("Within this distance from the camera a positional cue plays at full volume.")]
    public float nearHearingRadius = 10f;
    [Tooltip("Beyond this distance from the camera a positional cue is culled entirely.")]
    public float farHearingRadius = 24f;

    public Cue[] cues;
}
