using UnityEngine;

/// <summary>
/// Movement/feedback SFX for the player: footsteps paced by walk/run,
/// landing thumps gated by fall speed, hurt/death and heal cues via events.
/// One-shot actions (jump, dash, attack...) are triggered from PlayerController.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Damageable))]
public class PlayerSfxController : MonoBehaviour
{
    [Header("Footsteps")]
    public float walkStepInterval = 0.42f;
    public float runStepInterval = 0.28f;

    [Header("Landing")]
    [Tooltip("Minimum downward speed on touchdown before the landing sound plays.")]
    public float minLandFallSpeed = 4f;

    private Rigidbody2D rb;
    private TouchingDirections touching;
    private Damageable damageable;
    private PlayerController player;

    private float stepTimer;
    private bool wasGrounded = true;
    private float peakFallSpeed;

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
            if (peakFallSpeed < -minLandFallSpeed)
                Sfx.Play(SfxId.PlayerLand);
            peakFallSpeed = 0f;
        }

        UpdateFootsteps(grounded);
        wasGrounded = grounded;
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

        Sfx.Play(SfxId.PlayerFootstep);
        bool running = player != null && player.IsRunning;
        stepTimer = running ? runStepInterval : walkStepInterval;
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
