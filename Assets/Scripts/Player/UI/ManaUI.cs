using UnityEngine;
using UnityEngine.UI;

public class ManaUI : MonoBehaviour
    {
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]
    private PlayerStats stats;

    [SerializeField]
    public Image liquidImage;

    public static ManaUI Instance { get; private set; }

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


    public void SetVisualSoul(float currentMana, float maxMana)
    {
        liquidImage.fillAmount = (float)currentMana / maxMana;
    }

    private void Update()
    {
        SetVisualSoul(stats.Mana, stats.MaxMana);
    }
}
