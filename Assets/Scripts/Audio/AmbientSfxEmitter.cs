using UnityEngine;

/// <summary>
/// Proximity ambience for a world object (bell, shrine, torch, machinery...):
/// only audible when the camera is near, at a deliberately moderate volume,
/// fading in/out smoothly as the player comes and goes. Supports a continuous
/// loop (torch crackle, shrine hum), periodic one-shots (a bell that dings
/// every so often while you stand beside it), or both. Each emitter has its
/// own radii so a small torch can whisper while a big bell carries further.
/// </summary>
public class AmbientSfxEmitter : MonoBehaviour
{
    [Header("Loop")]
    [Tooltip("Optional continuous bed. Leave empty for periodic one-shots only.")]
    public AudioClip loopClip;

    [Header("Periodic one-shots")]
    [Tooltip("Optional clips played on a random interval while the emitter is audible.")]
    public AudioClip[] oneShotClips;
    [Tooltip("Random wait between one-shots, in seconds.")]
    public float minInterval = 5f;
    public float maxInterval = 11f;
    [Range(0.5f, 1.5f)] public float minPitch = 0.97f;
    [Range(0.5f, 1.5f)] public float maxPitch = 1.03f;

    [Header("Volume")]
    [Tooltip("Volume standing right next to it. Keep moderate — ambience sits under gameplay, never over it.")]
    [Range(0f, 1f)] public float maxVolume = 0.35f;
    [Tooltip("Full volume within this distance from the camera.")]
    public float nearRadius = 3f;
    [Tooltip("Inaudible beyond this distance from the camera.")]
    public float farRadius = 12f;
    [Tooltip("Seconds to fade the loop in/out when entering/leaving earshot.")]
    public float fadeTime = 0.6f;

    // Separate voices: pitch jitter on a one-shot must never warp the running loop.
    private AudioSource loopSource;
    private AudioSource oneShotSource;
    private float audibleWeight;
    private float nextOneShotIn;

    private void Awake()
    {
        loopSource = CreateVoice(true);
        loopSource.clip = loopClip;
        oneShotSource = CreateVoice(false);
        // PlayOneShot scales by source volume; loudness comes from the scale argument.
        oneShotSource.volume = 1f;

        nextOneShotIn = Random.Range(minInterval, maxInterval);
    }

    private AudioSource CreateVoice(bool loop)
    {
        AudioSource voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.loop = loop;
        voice.spatialBlend = 0f;    // falloff is done by hand so 2D camera distance rules
        voice.volume = 0f;
        voice.outputAudioMixerGroup = Sfx.OutputGroup;
        return voice;
    }

    private void OnValidate()
    {
        if (farRadius < nearRadius)
            farRadius = nearRadius;
        if (maxInterval < minInterval)
            maxInterval = minInterval;
        if (maxPitch < minPitch)
            maxPitch = minPitch;
    }

    private void Update()
    {
        float target = Attenuation();
        float step = fadeTime > 0f ? Time.unscaledDeltaTime / fadeTime : 1f;
        audibleWeight = Mathf.MoveTowards(audibleWeight, target, step);

        UpdateLoop();
        UpdateOneShots();
    }

    private void UpdateLoop()
    {
        if (loopClip == null)
            return;

        loopSource.volume = maxVolume * audibleWeight;

        if (audibleWeight > 0.01f && !loopSource.isPlaying)
        {
            // Random start offset so several identical emitters don't phase-lock.
            loopSource.time = Random.Range(0f, loopClip.length);
            loopSource.Play();
        }
        else if (audibleWeight <= 0.01f && loopSource.isPlaying)
        {
            loopSource.Stop();
        }
    }

    private void UpdateOneShots()
    {
        if (oneShotClips == null || oneShotClips.Length == 0)
            return;

        nextOneShotIn -= Time.deltaTime;
        if (nextOneShotIn > 0f)
            return;

        nextOneShotIn = Random.Range(minInterval, maxInterval);

        // Timer keeps running while out of earshot but only audible plays fire,
        // so walking up never triggers a burst of queued rings.
        if (audibleWeight <= 0.05f)
            return;

        AudioClip clip = oneShotClips[Random.Range(0, oneShotClips.Length)];
        if (clip == null)
            return;

        oneShotSource.pitch = Random.Range(minPitch, maxPitch);
        oneShotSource.PlayOneShot(clip, maxVolume * audibleWeight);
    }

    private float Attenuation()
    {
        Camera listener = Camera.main;
        if (listener == null)
            return 0f;

        float distance = Vector2.Distance(listener.transform.position, transform.position);
        if (distance <= nearRadius)
            return 1f;
        if (distance >= farRadius)
            return 0f;

        float remaining = 1f - Mathf.InverseLerp(nearRadius, farRadius, distance);
        return remaining * remaining;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, nearRadius);
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, farRadius);
    }
}
