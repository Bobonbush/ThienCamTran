using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    Damageable damagable;


    private int Mana = 0;
    private int MaxMana = 100;

    public int Health
    {
        get { return damagable.Health; }
    }

    public int MaxHealth
    {
        get { return damagable.MaxHealth; }
    }
    
    void Start()
    {
        damagable = GetComponent<Damageable>();
    }

}
