using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IngameStat : MonoBehaviour
{
    [SerializeField] private Image healthFill;
    [SerializeField] private Image manaFill;
    [SerializeField] private Image staminaFill;
    [SerializeField] private TMP_Text scoreText;

    private PlayerStats stats;
    private Inventory inventory;
    private RectTransform healthBar;
    private Vector3 healthBarScale;

    private void Awake()
    {
        UIRuntime.ConfigureCanvas(gameObject);
        if (healthFill == null) healthFill = UIRuntime.Find<Image>(transform, "Full_HP");
        if (manaFill == null) manaFill = UIRuntime.Find<Image>(transform, "Full_Mana");
        if (staminaFill == null) staminaFill = UIRuntime.Find<Image>(transform, "Full_Stamina");
        if (scoreText == null)
            scoreText = UIRuntime.FirstText(UIRuntime.Find(transform, "Point_Panel"), "Text (TMP)");

        healthBar = UIRuntime.Find(transform, "HP_Bar") as RectTransform;
        ConfigureFill(healthFill);
        ConfigureFill(manaFill);
        ConfigureFill(staminaFill);
        PrepareLeftAligned(healthBar, out healthBarScale);

        // The HUD is informational and must never consume pointer input intended for
        // the menu, dialogue, or item popup above it.
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
    }

    private void OnEnable() => BindPlayer();

    // Damageable has no health-changed event, therefore health is sampled every frame.
    private void Update() => Refresh();

    public void Bind(PlayerStats playerStats, Inventory playerInventory)
    {
        if (stats != null) stats.Changed -= Refresh;
        if (inventory != null) inventory.Changed -= Refresh;
        stats = playerStats;
        inventory = playerInventory;
        if (stats != null) stats.Changed += Refresh;
        if (inventory != null) inventory.Changed += Refresh;
        Refresh();
    }

    private void BindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) Bind(player.GetComponent<PlayerStats>(), player.GetComponent<Inventory>());
    }

    public void Refresh()
    {
        if (stats == null) return;
        if (healthFill != null) healthFill.fillAmount = Mathf.Clamp01((float)stats.Health / stats.MaxHealth);
        if (manaFill != null) manaFill.fillAmount = Mathf.Clamp01((float)stats.Mana / stats.MaxMana);
        if (staminaFill != null) staminaFill.fillAmount = Mathf.Clamp01(stats.Stamina / stats.MaxStamina);
        SetHorizontalScale(healthBar, healthBarScale, stats.MaxHealthScale, false);
        if (scoreText != null) scoreText.text = $"Điểm: {(inventory != null ? inventory.Score : 0)}";
    }

    private static void PrepareLeftAligned(RectTransform rect, out Vector3 authoredScale)
    {
        authoredScale = rect != null ? rect.localScale : Vector3.one;
        if (rect == null || Mathf.Approximately(rect.pivot.x, 0f)) return;
        Vector3[] before = new Vector3[4];
        Vector3[] after = new Vector3[4];
        rect.GetWorldCorners(before);
        rect.pivot = new Vector2(0f, rect.pivot.y);
        rect.GetWorldCorners(after);
        rect.position += before[0] - after[0];
    }

    private static void ConfigureFill(Image image)
    {
        if (image == null) return;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillClockwise = true;
        image.fillAmount = 1f;
    }

    private static void SetHorizontalScale(RectTransform rect, Vector3 authoredScale,
        float fraction, bool clamp = true)
    {
        if (rect == null) return;
        float value = clamp ? Mathf.Clamp01(fraction) : Mathf.Max(0.01f, fraction);
        rect.localScale = new Vector3(authoredScale.x * value, authoredScale.y, authoredScale.z);
    }

    private void OnDestroy()
    {
        if (stats != null) stats.Changed -= Refresh;
        if (inventory != null) inventory.Changed -= Refresh;
    }
}
