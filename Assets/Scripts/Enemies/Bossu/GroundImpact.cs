using UnityEngine;

public class GroundImpact : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveDistance = 6f;
    [SerializeField] private float moveDuration = 0.35f;

    [Header("Scale")]
    [SerializeField] private float maxScaleX = 5f;
    [SerializeField] private float startScaleY = 0.5f;

    [Header("Lifetime")]
    [SerializeField] private float destroyDelay = 0.2f;

    private Vector3 startPos;
    private Vector3 endPos;

    private Vector3 startScale;
    private Vector3 endScale;

    public void Initialize(float direction)
    {
        transform.position = new Vector3(transform.position.x, transform.position.y - startScaleY, transform.position.z);
        startPos = transform.position;
        endPos = startPos + Vector3.right * direction * -1 * moveDistance;

        startScale = new Vector3(0f, startScaleY, 1f);
        endScale = new Vector3(direction * maxScaleX, startScaleY, 1f);

        transform.localScale = startScale;

        StartCoroutine(GrowRoutine());
    }

    private System.Collections.IEnumerator GrowRoutine()
    {
        float timer = 0;

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / moveDuration);

            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        yield return new WaitForSeconds(destroyDelay);

        Destroy(gameObject);
    }
}

