using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.VisualScripting.Antlr3.Runtime.Misc;

public class TreUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField]
    public Sprite fullTreSprite;   // Tre still can be use

    [SerializeField]
    public Sprite usedTreSprite;   // used Tre which will lost its green color

    public GameObject TrePrefab;
    [SerializeField]
    PlayerStats playerStat;

    private List<Image> instanceTre = new List<Image>();
    
    public void SetupTre(int maxTre)
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
        instanceTre.Clear();

        for (int i = 0; i < maxTre; i++)
        {
            GameObject newTre = Instantiate(TrePrefab, transform);
            Image treImage = newTre.GetComponent<Image>();
            treImage.sprite = fullTreSprite;
            instanceTre.Add(treImage);
        }
    }

    private void Awake()
    {
        SetupTre(playerStat.Tre);
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < instanceTre.Count; i++)
        {
            if (i < playerStat.Tre)
            {
                instanceTre[i].sprite = fullTreSprite;
            }
            else
            {
                instanceTre[i].sprite = usedTreSprite;
            }
        }
    }
}
