using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
public class TorchFlicker : MonoBehaviour
{
    [SerializeField] private Light2D light2D;

    [Header("Intensity")]
    private float baseIntensity = 1.0f;
    private float intensityVariation = 0.1f;
    [SerializeField] private float smoothSpeed = 15f;

    [Header("Radius (very subtle)")]
    private float baseRadius = 4f;
    [SerializeField] private float radiusVariation = 0.03f;

    private float targetIntensity;
    private float targetRadius;

    void Start()
    {
        baseRadius = light2D.pointLightOuterRadius;

        targetIntensity = baseIntensity;
        targetRadius = baseRadius;

        StartCoroutine(FlickerRoutine());
    }

    void Update()
    {
        light2D.intensity = Mathf.Lerp(
            light2D.intensity,
            targetIntensity,
            smoothSpeed * Time.deltaTime
        );

        light2D.pointLightOuterRadius = Mathf.Lerp(
            light2D.pointLightOuterRadius,
            targetRadius,
            smoothSpeed * Time.deltaTime
        );
    }

    IEnumerator FlickerRoutine()
    {
        while (true)
        {

            float r = Random.value;

            if (r < 0.85f)
            {
                targetIntensity = baseIntensity + Random.Range(-0.15f, 0.05f);
            }
            else
            {
                targetIntensity = baseIntensity + Random.Range(0.08f, intensityVariation);
            }

            // Radius barely changes
            targetRadius = baseRadius + Random.Range(-radiusVariation, radiusVariation);

            yield return new WaitForSeconds(Random.Range(0.2f, 0.3f));
        }
    }
}
