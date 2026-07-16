using UnityEngine;

/// <summary>
/// Movement/feedback SFX for the player: surface-aware footsteps paced by
/// walk/run, landing thumps tiered by fall speed, ladder-climb ticks, and
/// hurt/death/heal cues via events.
/// One-shot actions (jump, dash, attack...) are triggered from PlayerController.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Damageable))]
public class PlayerSfxController : MonoBehaviour
{
    [Header("Footsteps")]
    public float walkStepInterval = 0.42f;
    public float runStepInterval = 0.28f;

    [Header("Climbing")]
    [Tooltip("Seconds between climb sounds while actually moving on a ladder.")]
    public float climbStepInterval = 0.34f;

    [Header("Landing")]
    [Tooltip("Minimum downward speed on touchdown before the landing sound plays.")]
    public float minLandFallSpeed = 4f;
    [Tooltip("Downward speed on touchdown at which the landing switches to the heavy variant.")]
    public float hardLandFallSpeed = 14f;

    private Rigidbody2D rb;
    private TouchingDirections touching;
    private Damageable damageable;
    private PlayerController player;

    private float stepTimer;
    private float climbTimer;
    private bool wasGrounded = true;
    private float peakFallSpeed;

    // One-entry cache: ground colliders change rarely compared to step rate.
    private Collider2D lastGroundCollider;
    private FootstepSurface.Surface lastSurface = FootstepSurface.Surface.Stone;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        touching = GetComponent<TouchingDirections>();
        damageable = GetComponent<Damageable>();
        player = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        damageable.damageableHit.AddListener(OnDamaged);
        CharacterEvents.characterHealed += OnHealed;
    }

    private void OnDisable()
    {
        damageable.damageableHit.RemoveListener(OnDamaged);
        CharacterEvents.characterHealed -= OnHealed;
    }

    private void Update()
    {
        bool grounded = touching.IsGrounded;

        if (!grounded)
        {
            peakFallSpeed = Mathf.Min(peakFallSpeed, rb.linearVelocityY);
        }
        else if (!wasGrounded)
        {
            PlayLanding();
            peakFallSpeed = 0f;
        }

        UpdateFootsteps(grounded);
        UpdateClimbSteps();
        wasGrounded = grounded;
    }

    private void PlayLanding()
    {
        float fallSpeed = -peakFallSpeed;
        if (fallSpeed < minLandFallSpeed)
            return;

        if (fallSpeed >= hardLandFallSpeed)
        {
            Sfx.Play(SfxId.PlayerLandHard);
            return;
        }

        // Faster fall -> heavier thump: scale volume up and pitch down across the tier.
        float weight = Mathf.InverseLerp(minLandFallSpeed, hardLandFallSpeed, fallSpeed);
        Sfx.Play(SfxId.PlayerLand, Mathf.Lerp(0.7f, 1f, weight), Mathf.Lerp(1.05f, 0.92f, weight));
    }

    private void UpdateFootsteps(bool grounded)
    {
        bool stepping = grounded
            && damageable.IsAlive
            && Mathf.Abs(rb.linearVelocityX) > 0.1f
            && (player == null || (player.IsMoving && !player.IsDashing));

        if (!stepping)
        {
            // Small lead-in so the first step lands right after movement starts.
            stepTimer = 0.06f;
            return;
        }

        stepTimer -= Time.deltaTime;
        if (stepTimer > 0f)
            return;

        bool running = player != null && player.IsRunning;
        // Running digs in harder than walking.
        Sfx.Play(FootstepCue(), running ? 1f : 0.85f);
        stepTimer = running ? runStepInterval : walkStepInterval;
    }

    private void UpdateClimbSteps()
    {
        bool climbing = player != null
            && player.IsClimbing
            && damageable.IsAlive
            && Mathf.Abs(rb.linearVelocityY) > 0.1f;

        if (!climbing)
        {
            climbTimer = 0.05f;
            return;
        }

        climbTimer -= Time.deltaTime;
        if (climbTimer > 0f)
            return;

        Sfx.Play(SfxId.PlayerClimb);
        climbTimer = climbStepInterval;
    }

    private string FootstepCue()
    {
        switch (CurrentSurface())
        {
            case FootstepSurface.Surface.Grass: return SfxId.PlayerFootstepGrass;
            case FootstepSurface.Surface.Wood: return SfxId.PlayerFootstepWood;
            case FootstepSurface.Surface.Water: return SfxId.PlayerFootstepWater;
            default: return SfxId.PlayerFootstep;
        }
    }

    private FootstepSurface.Surface CurrentSurface()
    {
        Collider2D ground = touching.GroundCollider;
        if (ground == null)
            return FootstepSurface.Surface.Stone;

        if (ground != lastGroundCollider)
        {
            lastGroundCollider = ground;
            FootstepSurface surface = ground.GetComponentInParent<FootstepSurface>();
            lastSurface = surface != null ? surface.surface : FootstepSurface.Surface.Stone;
        }

        return lastSurface;
    }

    private void OnDamaged(int damage, Vector2 knockback)
    {
        Sfx.Play(damageable.IsAlive ? SfxId.PlayerHurt : SfxId.PlayerDeath);
    }

    private void OnHealed(GameObject healed, int amount)
    {
        if (healed == gameObject)
            Sfx.Play(SfxId.PlayerHeal);
    }
}
