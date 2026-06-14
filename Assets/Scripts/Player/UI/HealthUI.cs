using UnityEngine;
using UnityEngine.UI;
public class HealthUI : MonoBehaviour
{

    [SerializeField] 
    public Image liquidImage;

    [SerializeField]
    PlayerStats playerStats;

    public static HealthUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void SetVisualSoul(float currentSoul, float maxSoul)
    {
        // This sets the fill between 0.0 and 1.0 safely
        liquidImage.fillAmount = currentSoul / maxSoul;
    }

    private void Update()
    {
        SetVisualSoul(playerStats.Health, playerStats.MaxHealth);
    }

}
