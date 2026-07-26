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
        public List<TrapInfo> trapInfos = new();    
    }

    //Custom Traps
    public List<TrapRound> trapList = new List<TrapRound>();

    private int round = -1;

    private int trapCounter = 0;

    private bool start = false;

    private List<GameObject> currentEnemies  = new List<GameObject>();
    private List<GameObject> currentTrapList = new List<GameObject>();

    private PlayerController playerController;
    private PlayerCamera playerCamera;


    private CutTrigger cut;
    private bool LastenemiesFinish = false;


    private ActivateTrap activeTrap; // use for close the door

    Coroutine spawning = null;

    private void Awake()
    {
        activeTrap = GetComponent<ActivateTrap>();
        cut = GetComponent<CutTrigger>();
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


    public void ActiveAllTrap()
    {
        for (int cnt = 0; cnt < trapList.Count; cnt++)
        {
            for (int i = 0; i < trapList[cnt].trapInfos.Count; i++)
            {
                TrapInfo info = trapList[cnt].trapInfos[i];
                info.traps.ActivateTrap();
                
            }
        }
        ActivateTrap(false);
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
            GameObject enemy = Instantiate(
                 info.enemiesPrefab,
                 actualTransform.position + info.spawnOffset,
                 Quaternion.identity // or spawn.rotation if you want it too
             );

            if (actualTransform.lossyScale.x < 0.0f)
            {
                enemy.GetComponent<EnemyMove>().Flip();
            }
            currentEnemies.Add(enemy);
            Sfx.PlayAt(SfxId.EnemySpawn, actualTransform.position);
        }
        spawning = null;
    }

    public IEnumerator ActivateAdditionalTraps()
    {
        if (trapList[trapCounter].round != round) yield break;

        yield return new WaitForSeconds(trapList[trapCounter].delayTime);

        currentTrapList.Clear();
        for (int i = 0; i < trapList[trapCounter].trapInfos.Count; i++)
        {
            TrapInfo info = trapList[trapCounter].trapInfos[i];
            info.traps.ActivateTrap();
        }
        trapCounter++;
        spawning = null;
    }


    private bool isEnemiesDone()
    {
        for (int i = 0; i < currentEnemies.Count; i++)
        {
            if (currentEnemies[i] != null) return false;
        }
        return true;
    }

    private bool isTrapDone()
    {
        for(int i = 0; i< currentTrapList.Count; i++)
        {
            if (currentTrapList[i] != null) return false;
        }
        return true;
    }

    private bool isDone()
    {
        bool isEFinish = isEnemiesDone();
        if(isEFinish && LastenemiesFinish == false)
        {
            spawning = StartCoroutine(ActivateAdditionalTraps());
        }
        LastenemiesFinish = isEFinish;
        return isEFinish && LastenemiesFinish && isTrapDone();
    }

    private void Done()
    {
        if(cut != null)
        {
            cut.Trigger(playerController, playerCamera);
        }

        
        ActivateTrap(false);

        this.enabled = false;
    }

    public void SetUpCutScene(PlayerController controller , PlayerCamera p_camera)
    {
        playerController = controller;
        playerCamera = p_camera;
    }
}
