using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemySfxController : MonoBehaviour
{
    [Header("Awareness")]
    [Tooltip("Bark played once when the enemy first spots the player (hasTarget animator bool flips on).")]
    public AudioClip[] alertClips;
    [Tooltip("Alert barks use their own volume — raw creature clips run hotter than combat foley.")]
    [Range(0f, 1f)] public float alertVolume = 0.55f;
    [Tooltip("Minimum seconds after losing the target before the enemy barks again on re-acquire.")]
    public float realertCooldown = 4f;

    [Header("Movement")]
    [Tooltip("Optional footsteps; leave empty for enemies that don't walk (plants, turrets).")]
    public AudioClip[] footstepClips;
    public float footstepInterval = 0.38f;
    [Range(0f, 1f)] public float footstepVolume = 0.4f;

    [Header("Combat")]
    public AudioClip[] shieldRaiseClips;
    public AudioClip[] attackWindupClips;
    public AudioClip[] attackReleaseClips;
    public AudioClip[] attackImpactClips;
    public AudioClip[] attackImpactLayerClips;
    public AudioClip[] blockClips;
    public AudioClip[] catchClips;

    [Header("Damage")]
    public AudioClip[] hurtClips;
    public AudioClip[] deathClips;

    [Header("Playback")]
    [Range(0f, 1f)] public float volume = 0.85f;
    [Range(0f, 1f)] public float impactLayerVolume = 0.55f;
    [Range(0.5f, 1.5f)] public float minimumPitch = 0.94f;
    [Range(0.5f, 1.5f)] public float maximumPitch = 1.06f;
    [Range(0f, 1f)] public float spatialBlend;
    [Tooltip("Quiet down / cull cues while this enemy is far off-screen, like Sfx.PlayAt does.")]
    public bool distanceFalloff = true;

    private AudioSource audioSource;
    private Damageable damageable;
    private Animator animator;
    private Rigidbody2D rb;
    private TouchingDirections touching;
    private AudioClip previousClip;

    private bool hasTargetParam;
    private bool hadTarget;
    private float targetLostAt = float.NegativeInfinity;
    private float footstepTimer;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        damageable = GetComponent<Damageable>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        touching = GetComponent<TouchingDirections>();
        ConfigureAudioSource();

        // Every enemy AI drives the hasTarget animator bool, so awareness barks
        // need no per-enemy wiring — just poll the parameter if it exists.
        if (animator != null)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.name == AnimationStrings.hasTarget)
                {
                    hasTargetParam = true;
                    break;
                }
            }
        }
    }

    private void OnEnable()
    {
        if (damageable != null)
            damageable.damageableHit.AddListener(OnDamaged);
    }

    private void OnDisable()
    {
        if (damageable != null)
            damageable.damageableHit.RemoveListener(OnDamaged);
    }

    private void Update()
    {
        UpdateAlert();
        UpdateFootsteps();
    }

    private void UpdateAlert()
    {
        if (!hasTargetParam)
            return;

        bool hasTarget = animator.GetBool(AnimationStrings.hasTarget);

        if (hasTarget && !hadTarget && Time.time >= targetLostAt + realertCooldown)
            PlayCue(alertClips, alertVolume);

        if (!hasTarget && hadTarget)
            targetLostAt = Time.time;

        hadTarget = hasTarget;
    }

    private void UpdateFootsteps()
    {
        bool stepping = footstepClips != null && footstepClips.Length > 0
            && rb != null
            && Mathf.Abs(rb.linearVelocityX) > 0.1f
            && (touching == null || touching.IsGrounded)
            && (damageable == null || damageable.IsAlive);

        if (!stepping)
        {
            footstepTimer = 0.05f;
            return;
        }

        footstepTimer -= Time.deltaTime;
        if (footstepTimer > 0f)
            return;

        PlayCue(footstepClips, footstepVolume);
        footstepTimer = footstepInterval;
    }

    private void OnValidate()
    {
        if (maximumPitch < minimumPitch)
            maximumPitch = minimumPitch;

        AudioSource source = GetComponent<AudioSource>();
        if (source != null)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = spatialBlend;
        }
    }

    public void PlayShieldRaise()
    {
        PlayCue(shieldRaiseClips);
    }

    public void PlayAttackWindup()
    {
        PlayCue(attackWindupClips);
    }

    public void PlayAttackRelease()
    {
        PlayCue(attackReleaseClips);
    }

    public void PlayAttackImpact()
    {
        PlayCue(attackImpactClips);
        PlayCue(attackImpactLayerClips, impactLayerVolume);
    }

    public void PlayBlock()
    {
        PlayCue(blockClips);
    }

    public void PlayCatch()
    {
        PlayCue(catchClips);
    }

    public void PlayAlert()
    {
        PlayCue(alertClips, alertVolume);
    }

    private void OnDamaged(int damage, Vector2 knockback)
    {
        if (damageable != null && damageable.IsAlive)
            PlayCue(hurtClips);
        else
            PlayCue(deathClips);
    }

    private void ConfigureAudioSource()
    {
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = spatialBlend;
        if (audioSource.outputAudioMixerGroup == null)
            audioSource.outputAudioMixerGroup = Sfx.OutputGroup;
    }

    private void PlayCue(AudioClip[] clips)
    {
        PlayCue(clips, volume);
    }

    private void PlayCue(AudioClip[] clips, float cueVolume)
    {
        if (distanceFalloff)
        {
            cueVolume *= Sfx.DistanceScale(transform.position);
            if (cueVolume <= 0.01f)
                return;
        }

        AudioClip clip = ChooseClip(clips, previousClip);
        if (clip == null)
            return;

        previousClip = clip;
        audioSource.pitch = Random.Range(minimumPitch, maximumPitch);
        audioSource.PlayOneShot(clip, cueVolume);
    }

    public static void PlayAtPoint(AudioClip[] clips, Vector3 position, float volume = 1f,
        float minimumPitch = 0.96f, float maximumPitch = 1.04f)
    {
        volume *= Sfx.DistanceScale(position);
        if (volume <= 0.01f)
            return;

        AudioClip clip = ChooseClip(clips, null);
        if (clip == null)
            return;

        GameObject oneShot = new GameObject("Enemy SFX One Shot");
        oneShot.transform.position = position;

        AudioSource source = oneShot.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = Sfx.OutputGroup;
        source.volume = Mathf.Clamp01(volume);
        source.pitch = Random.Range(minimumPitch, maximumPitch);
        source.clip = clip;
        source.Play();

        Destroy(oneShot, clip.length / Mathf.Max(0.01f, Mathf.Abs(source.pitch)) + 0.05f);
    }

    private static AudioClip ChooseClip(AudioClip[] clips, AudioClip clipToAvoid)
    {
        if (clips == null || clips.Length == 0)
            return null;

        int validCount = 0;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
                validCount++;
        }

        if (validCount == 0)
            return null;

        int selectedValidIndex = Random.Range(0, validCount);
        AudioClip selectedClip = null;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null)
                continue;

            if (selectedValidIndex-- == 0)
            {
                selectedClip = clips[i];
                break;
            }
        }

        if (validCount == 1 || selectedClip != clipToAvoid)
            return selectedClip;

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i] != clipToAvoid)
                return clips[i];
        }

        return selectedClip;
    }
}
