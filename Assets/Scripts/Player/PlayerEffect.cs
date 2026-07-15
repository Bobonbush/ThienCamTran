using UnityEngine;
using System.Collections;
public class PlayerEffect : MonoBehaviour
{
    private ParticleSystem bloodPrefab;
    private SpriteRenderer spriteRenderer;

    [SerializeField] private Vector3 offsetBlood = Vector3.zero;

    private Material originalMaterial;
    private Material flashMaterial;
    private Coroutine flashRoute;

    [SerializeField] private float duration;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        
        bloodPrefab = Resources.Load<ParticleSystem>("Effect/Blood");
        spriteRenderer = GetComponent<SpriteRenderer>();
        flashMaterial = Resources.Load<Material>("Material/WhiteOutShader");
        originalMaterial = spriteRenderer.material;
    }


    public void BloodEffect(Vector2 hit_direction)
    {


        Quaternion rotation = bloodPrefab.transform.rotation;

        if (hit_direction.x > 0)
        {
            rotation *= Quaternion.Euler(0, 180, 0);
        }
        Instantiate(
             bloodPrefab,
             transform.position + offsetBlood,
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
}
