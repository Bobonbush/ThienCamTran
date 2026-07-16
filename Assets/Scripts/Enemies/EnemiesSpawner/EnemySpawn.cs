using NUnit.Framework;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class EnemySpawn : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [System.Serializable]
    public struct EnemiesInfo
    {
        public GameObject enemiesPrefab;
        public GameObject spawnPrefab;
        public Vector3 spawnOffset;
    };


    [SerializeField]
    private bool useActivator = true;


    [System.Serializable]
    public class EnemyRound
    {
        public float delayTime = 0.5f;
        public List<EnemiesInfo> enemies = new();
    }
    // Round and Enemies;
    [SerializeField]
    public List<EnemyRound> enemyList = new List<EnemyRound>();



    [System.Serializable]
    
    public class TrapInfo
    {
        public TrapMove traps;
    }


    [System.Serializable]
    public class TrapRound
    {
        // which round done for activate these traps
        public int round = 0;
        public float delayTime = 0.5f;
        List<TrapInfo> trapInfos = new();    
    }

    public List<TrapRound> trapList = new List<TrapRound>();

    private int round = -1;

    private bool start = false;

    public List<GameObject> currentEnemies;
    


    private ActivateTrap activeTrap; // use for close the door

    Coroutine spawning = null;

    private void Awake()
    {
        activeTrap = GetComponent<ActivateTrap>();
    }

    private void Start()
    {
        trapList.Sort((a, b) => a.round.CompareTo(b.round));
    }


    public void StartUp()
    {
        start = true;
    }

    private void Update()
    {
        if (start) Spawn();
    }


    private void ActivateTrap(bool active)
    {
        if (useActivator == false) return;
        if(activeTrap != null)
        {
            if (active)
                activeTrap.ActivateTraps();
            else
                activeTrap.DeActiveTraps();
        }
    }


    public void Spawn()
    {
        if(isDone() && spawning == null)
        {
            round++;
            if(round == 0)
            {
                ActivateTrap(true);
            }


            if(round == enemyList.Count)
            {
                Done();
            }else
            {
                spawning = StartCoroutine(SummonEnemy());
            }
        }
    }

    public IEnumerator SummonEnemy()
    {
        yield return new WaitForSeconds(enemyList[round].delayTime);

        currentEnemies.Clear();
        for (int i = 0; i < enemyList[round].enemies.Count; i++)
        {
            EnemiesInfo info = enemyList[round].enemies[i];
            Transform actualTransform = info.spawnPrefab.transform;
            actualTransform.position += info.spawnOffset;
            currentEnemies.Add(Instantiate(info.enemiesPrefab, actualTransform));
            Sfx.PlayAt(SfxId.EnemySpawn, actualTransform.position);
        }
        spawning = null;
    }



    private bool isDone()
    {
        for(int i = 0; i < currentEnemies.Count; i++)
        {
            if (currentEnemies[i] != null) return false;
        }
        return true;
    }

    private void Done()
    {
        Debug.Log("Done");
        ActivateTrap(false);

        this.enabled = false;
    }
}
