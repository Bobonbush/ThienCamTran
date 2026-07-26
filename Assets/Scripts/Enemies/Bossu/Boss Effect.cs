using UnityEngine;
using System.Collections;

public class BossEffect : MonoBehaviour
{
    private ParticleSystem bloodPrefab;
    private SpriteRenderer spriteRenderer;

    [SerializeField] private Vector3 offsetBlood = Vector3.zero;

    private Material originalMaterial;
    private Material flashMaterial;
    private Coroutine flashRoute;

    [SerializeField] private float duration;

    PlayerCamera playerCamera;

    private Bossu boss;
    bool flip = false;

    private Vector3 worldPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {

        bloodPrefab = Resources.Load<ParticleSystem>("Effect/Blood");
        spriteRenderer = GetComponent<SpriteRenderer>();
        flashMaterial = Resources.Load<Material>("Material/WhiteOutShader");
        originalMaterial = spriteRenderer.material;
        playerCamera = CutSceneManager.Instance.playerCamera;
        boss = GetComponentInParent<Bossu>();
    }

    private void Update()
    {
        if(flip)
        {
            transform.localPosition = worldPosition;
            flip = false;
        }
    }


    public void BloodEffect(Vector2 hit_direction)
    {


        Quaternion rotation = bloodPrefab.transform.rotation;

        if (hit_direction.x > 0)
        {
            rotation *= Quaternion.Euler(0, 180, 0);
        }

        Transform parentTransform = transform.parent;

        Instantiate(
             bloodPrefab,
             parentTransform.position + offsetBlood,
             rotation
        );
    }

    public void Flash()
    {
        if (flashRoute != null)
        {
            StopCoroutine(flashRoute);
        }

        flashRoute = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        spriteRenderer.material = flashMaterial;

        yield return new WaitForSeconds(duration);

        spriteRenderer.material = originalMaterial;


        flashRoute = null;
    }

    public void EarthWake(float duration)
    {
        StartCoroutine(playerCamera.Earthquake(duration));
    }

    public void EarthWakeOnDown()
    {
        // Heavy slam when the boss body hits the arena floor
        Sfx.PlayAt(SfxId.BossSlam, transform.position);
        EarthWake(0.3f);
    }

    public void EarthWakeOnJump()
    {
        // Lighter thump for the push-off
        Sfx.PlayAt(SfxId.BossSlam, transform.position, 0.6f);
        EarthWake(0.3f);
    }

    public void Flip()
    {
        worldPosition = transform.localPosition;
        boss.AnimationFlip();
        flip = true;
    }

    public void SpawnImpactAnimation()
    {
        boss.SpawnGroundImpact();
    }

    // Dash further to the right

    public void GroundStabDash()
    {
        boss.GroundStabDash();
    }

    public void SpawnGroundImpactOnBoth()
    {
        boss.SpawnGroundImpactOnBoth();
    }


    public void GroundStabEnd()
    {
        boss.GroundStabEnd();
    }

    // Dash to surprise the player
    public void StabDash()
    {
        boss.StabDash();
    }

    public void ReadyForStab()
    {
        boss.ReadyForStab();
    }

    public void ListenStabEnd()
    {
        boss.ListenStabEnd();
    }



    public void StabEnd()
    {
        boss.StabEnd();
    }

    public void DoneFalling()
    {
        boss.DoneFalling();
    }

    public void SpecialUpAbility()
    {
        boss.SpecialUpAbility();
    }

    

    public void AnimationDeadBoss()
    {
        boss.AnimationDeadBoss();
    }
}
