using UnityEngine;

// Bóng đen assassin để lại khi tàng hình: mờ dần rồi tự huỷ
[RequireComponent(typeof(SpriteRenderer))]
public class AssassinShadow : MonoBehaviour
{
    [Min(0.05f)] public float lifetime = 0.8f;

    SpriteRenderer spriteRenderer;
    float age;
    float startAlpha;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startAlpha = spriteRenderer.color.a;
    }

    private void Update()
    {
        age += Time.deltaTime;

        Color color = spriteRenderer.color;
        color.a = Mathf.Lerp(startAlpha, 0f, age / lifetime);
        spriteRenderer.color = color;

        if (age >= lifetime)
            Destroy(gameObject);
    }
}
