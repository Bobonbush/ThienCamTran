using UnityEngine;
using UnityEngine.UI;

// Compatibility adapter for old scenes. New UI is owned by IngameStat.
public class HealthUI : MonoBehaviour
{
    [SerializeField] private Image liquidImage;
    [SerializeField] private PlayerStats playerStats;
    private void Update()
    {
        if (liquidImage != null && playerStats != null)
            liquidImage.fillAmount = Mathf.Clamp01((float)playerStats.Health / playerStats.MaxHealth);
    }
}
