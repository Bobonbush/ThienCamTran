using NUnit.Framework;
using Unity.VisualScripting;
using UnityEditor.AssetImporters;
using UnityEditor.Rendering;
using UnityEngine;
using System.Collections.Generic;
using System.Security.Cryptography;

public class Bossu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    const int INF = 100000000;
    private Animator anim;

    private Damageable damageable;
    private BossEffect effect;
    private Rigidbody2D rb;
    private TouchingDirections touching;
    private bool Released = false;

    public float gravityScale = 3.0f;

    Vector2 boundX = new Vector2(-27.04f, 15.63f);

    private Transform playerTransform;


    // Release for debug
    [SerializeField]
    private bool RelasedSoon = false;

    [SerializeField]
    private float offsetX = 2.0f;

    private float speedChase = 5.0f;

    public enum BossState
    {
        Idle,
        Chase,
        Stab,
        StabtoGround,
        Summon,
        SpecialAbility,
        Hurt,
        Dead,
        Fall,
        DisapearSpawn
    }

    public enum SpellState
    {
        Stab,
        StabtoGround, 
        Frenzy,
        SpecialAbility
    };

    private BossState state = BossState.Fall;

    private bool stateisDone = false;

    private Vector2 _walkDiretionVector = Vector2.left;


    public Vector2 walkDiretionVector
    {
        get { return _walkDiretionVector; }
        private set { _walkDiretionVector = value; }
    }


    public bool CanMove
    {
        get
        {
            return anim.GetBool(AnimationStrings.canMove);
        }
    }


    private void Awake()
    {

        anim = GetComponentInChildren<Animator>();
        damageable = GetComponent<Damageable>();
        rb = GetComponent<Rigidbody2D>();
        effect = GetComponentInChildren<BossEffect>();
        touching = GetComponent<TouchingDirections>();

        originDamping = rb.linearDamping;
    }


    private void Start()
    {
        playerTransform = CutSceneManager.Instance.playerTransform;
        if(RelasedSoon)
        {
            Release();
        }
    }


    void Flip(Vector3 dirX)
    {
        // Flip too fast
        if(playerTransform.position.y > transform.position.y - 1f)
        {
            return;
        }
        if(dirX.x > 0 && walkDiretionVector.x < 0 )
        {
            _walkDiretionVector *= -1;
            anim.SetTrigger(AnimationStrings.isSwapDir);
        }else if(dirX.x < 0 && walkDiretionVector.x > 0)
        {
            _walkDiretionVector *= -1;
            anim.SetTrigger(AnimationStrings.isSwapDir);
        }
    }

    // Follow the player

    private float ChasingTime = 0.0f;
    private float MaxChasingTime = 3.0f;

    private float CrawlCurrentStateTime = 0.0f;
    private float CrawlEndStateTime = 1.0f;

    private float rest = 0.25f;
    private float maxRest = 0.25f;
    private float speed = 3.0f;

    private float originDamping = 0.0f;

    

    private bool OnSpellCast = false;

    private  void Chase()
    {
        Flip(playerTransform.position - transform.position);
        Crawl();
    }

    private void Crawl()
    {
        ChasingTime += Time.deltaTime;
        
        if (playerTransform.position.y > transform.position.y - 1f)
        {
            return;
        }

        float constantValue = 1.0f;

        if (damageable.hpPercentage <= 0.75)
        {
            constantValue = 0.8f;
        }
        if (damageable.hpPercentage < 0.5f)
        {
            constantValue = 0.65f;
        }

        if(damageable.hpPercentage < 0.35f)
        {
            constantValue = 0.5f;
        }

        if (ChasingTime > MaxChasingTime * constantValue)
        {
            SetUpAttack();
            rb.linearVelocityX = 0.0f;
            return;
        }
        float desiredPosition = transform.position.x - playerTransform.position.x;

        
        if (Mathf.Abs(desiredPosition) < offsetX * 1.25 && ChasingTime >= MaxChasingTime * constantValue / 2.0f)
        {
            rb.linearDamping = originDamping;
            SetUpAttack();
            rb.linearVelocityX = 0.0f;
            return;
        }


        float constantRest = 1.0f;
        if(damageable.hpPercentage <= 0.75)
        {
            constantRest = 0.8f;
        }
        if(damageable.hpPercentage < 0.5f)
        {
            constantRest = 0.5f;
        }
        if (rest < maxRest * constantRest)
        {
            rest += Time.deltaTime;
            rb.linearDamping = 2f;
            
            if(rest >= maxRest)
            {
                CrawlCurrentStateTime = 0.0f;
            }
            return;
        }
        if (Mathf.Abs(desiredPosition) > offsetX)
        {
            if (CrawlCurrentStateTime < CrawlEndStateTime)
            {
                rb.linearVelocityX = speed * transform.localScale.x;
                CrawlCurrentStateTime += Time.deltaTime;
                if (CrawlCurrentStateTime >= CrawlEndStateTime)
                {
                    rest = 0.0f;
                }
            }
        }else
        {
            rb.linearVelocityX = 0.0f;
        }
    }


    // Recovering like 0.5 seconds for player to attack.
    private float idleTime = 0;
    private float idleMaxTime = 0.5f; // Set for Default;
    private void Idle()
    {
        Flip(playerTransform.position - transform.position);
        idleTime += Time.deltaTime;
        if(idleTime > idleMaxTime)
        {
            state = BossState.Chase;
            ChasingTime = 0.0f;
        }
    }

    private bool firstStab = false;


    private void StartStab()
    {
        OnSpellCast = true;
        stabbed = false;
        anim.SetTrigger("SimpleAttack");
        numberOfStab = 1;
        firstStab = false;
        if(damageable.hpPercentage < 0.75)
        {
            numberOfStab = Random.Range(2, 5);
        }
        if(damageable.hpPercentage < 0.35)
        {
            numberOfStab = Random.Range(3, 5);
        }
        stabTiming = 0.0f;
    }

    private float stabTiming = 0.0f;
    private float stabmaxTime = 0.5f;
    private bool stabbed = false;
    
    private int numberOfStab = 1;

    private void StabUpdate()
    {
        stabTiming += Time.deltaTime;
        float constantTime = 1.0f;

        if (damageable.hpPercentage < 0.75)
        {
            constantTime = Random.Range(0.75f, 0.8f);
        }

        if(damageable.hpPercentage < 0.45)
        {
            constantTime = Random.Range(0.7f, 0.75f);
        }

        
        if (stabTiming > stabmaxTime * constantTime && stabbed == false)
        {
            stabbed = true;
            anim.SetTrigger("Stab");
        }
    }
   
    public void ReadyForStab()
    {
        state = BossState.Stab;
    }

    public void ListenStabEnd()
    {
        numberOfStab--;
        if (numberOfStab > 0)
        {
            anim.SetTrigger("Stab");
        }
    }
    public void StabEnd()
    {
        SetIdle(1.0f); 
    }

    // 3 phases : 1 is the boss jump up, particles like dirts will fall to the ground dealing damage to the player, 2 choose the player position and wait for seconds, 3 fall down to that position and once again dirts fall.

    public void StabDash()
    {
        if(firstStab)
        {
            return;
        }
        firstStab = true;
        float dashImplitude = playerTransform.position.x - transform.position.x;
        
        if(Mathf.Abs(dashImplitude) <= offsetX * 1.25f)
        {
            return;
        }
        float aspect = (offsetX * 1.25f) / Mathf.Abs(dashImplitude);

        
        transform.position = new Vector3(transform.position.x + dashImplitude * aspect * 0.25f, transform.position.y, transform.position.z);
    }


    
    private void StartSpecialAbility()
    {
        OnSpellCast = true;
        anim.SetTrigger("SpecialAttack");
        SpecialUpDown = false;
    }

    public void SpecialUpAbility()
    {

        rb.gravityScale = -2;
        if (damageable.hpPercentage < 0.75) rb.gravityScale = -4;
        if (damageable.hpPercentage < 0.5) rb.gravityScale = -6;

        rb.AddForce(new Vector2(0.0f, 5.0f));

        if(state == BossState.DisapearSpawn && SpecialType.Frenzy == specialType)
        {
            return;
        }

        state = BossState.DisapearSpawn;

        if(specialType == SpecialType.Frenzy)
        {
            endSpecialTime = 1.5f;
            maximalRock = Random.Range(1, 3);
            timePerRockThrow = 0.5f;
            timePerRockThrow = endSpecialTime / (maximalRock + 1.0f);
        }
        else
        {
            endSpecialTime = Random.Range(2.5f, 3.5f);
            
            timePerRockThrow = 1.0f;
            maximalRock = Random.Range(1, 3);
            timePerRockThrow = endSpecialTime / (maximalRock + 1.0f);
        }

        startSpecialTime = 0.0f;
        timeRock = 0.0f;
        totalRock = 0;
        

    }


    private bool SpecialUpDown = false;

    private float startSpecialTime = 0.0f;
    private float endSpecialTime = 4.0f;

    private float timePerRockThrow = 2.0f * 2/3f;
    private float timeRock = 0.0f;
    private int totalRock = 0;
    private int maximalRock = 2;

    private float frenzyTime = 3.0f;
    
    float range = 32.0f;
    enum SpecialType {
        EarthWake,
        Frenzy
    };

    SpecialType specialType = SpecialType.EarthWake;

    
    [SerializeField] private GameObject dirtPrefab;

    private void SpawnRock()
    {
        timeRock += Time.deltaTime;
        if(timeRock < timePerRockThrow || totalRock > maximalRock)
        {
            
            return;
        }
        totalRock++;
        timeRock = 0.0f;


        float scalePercentage = damageable.hpPercentage;

        scalePercentage = Mathf.Clamp(scalePercentage, 0.1f, 1.0f);

        int rockCount = Random.Range((int)(5 * (0.5f / scalePercentage)), (int) (8 * (0.35f/ scalePercentage)));

        float left = Mathf.Max(playerTransform.position.x - range * 0.5f / scalePercentage, boundX.x);
        float right = Mathf.Min(playerTransform.position.x + range * 0.5f / scalePercentage, boundX.y);

        float width = right - left;
        float sectionWidth = width / rockCount;

        for (int i = 0; i < rockCount; i++)
        {
            float x = left + i * sectionWidth +
              Random.Range(0f, sectionWidth);

            Vector3 spawnPos = new Vector3(x, Random.Range(16.0f, 32.0f), 0);

            GameObject dirt = Instantiate(dirtPrefab, spawnPos, Quaternion.identity);
            dirt.GetComponent<ProjectileTrajectory>().maximumFallSpeed = Random.Range(12, 20);
        }
    }


  
    public void SpecialUpUpdate()
    {

        float limitheight = 15.96f;
        if (transform.position.y > limitheight && SpecialUpDown == false )
        {
            rb.gravityScale = 0;
            rb.linearVelocityY = 0;
            SpecialUpDown = true;
            frenzyTime = 0.25f;
            transform.position = new Vector3(transform.position.x, limitheight + 1.0f, transform.position.z);
        }

        

        if(SpecialUpDown == false)
        {

            return;
        }




        
        if (startSpecialTime > endSpecialTime)
        {
            
            return;
        }

        if (transform.position.y <= limitheight)
        {
            return;
        }

        startSpecialTime += Time.deltaTime;
        SpawnRock();
        effect.EarthWake(0.15f);
        
        // Perform special attack

        if(frenzyTime > 0.0f)
        {
            frenzyTime -= Time.deltaTime;
        }else if(specialType == SpecialType.Frenzy)
        {
            EndSpecialAttack();
            frenzyTime = 10.0f;
        }

        if (startSpecialTime > endSpecialTime)
        {
            EndSpecialAttack();
        }
    }

    private void EndSpecialAttack()
    {
       
        anim.SetTrigger("GoDown");
        rb.gravityScale = 3;
        if (damageable.hpPercentage < 0.75) rb.gravityScale = 4.0f;
        if (damageable.hpPercentage < 0.5) rb.gravityScale = 5.0f;

        float xPos = playerTransform.position.x;
        xPos = Mathf.Clamp(xPos, boundX.x + 0.5f, boundX.y - 0.5f);
        transform.position = new Vector3(xPos, transform.position.y, transform.position.z);
    }


    
    public void SpawnGroundImpactOnBoth()
    {
        if (CutSceneManager.Instance.IsInCutScene) return;

        float direction = Mathf.Sign(transform.localScale.x);

        direction *= -1;

        GroundImpact impact = Instantiate(
            groundImpactPrefab,
            new Vector3(transform.root.position.x, -1.05f, transform.root.position.z),
            Quaternion.identity);


        GroundImpact _impact = Instantiate(
            groundImpactPrefab,
            new Vector3(transform.root.position.x, -1.05f, transform.root.position.z),
            Quaternion.identity);
        _impact.Initialize(-direction);
        impact.Initialize(direction);
    }


    public void SummonParticle()
    {

    }

    private void SpecialDownAbility()
    {

    }

    private void OnSpecialAbility()
    {
        SetIdle(1.0f);
    }



    // Stab to the ground create a shock wave toward the player, look like the failed knight in hollow knight

    [SerializeField] private GroundImpact groundImpactPrefab;
    [SerializeField] private Transform stabPoint;

    private void StartGroundStab()
    {
        OnSpellCast = true;
        anim.SetTrigger("StabOnGround");
        if(damageable.hpPercentage < 0.75)
            anim.SetFloat("GroundStabSpeed", 1.5f);
        if (damageable.hpPercentage < 0.5)
            anim.SetFloat("GroundStabSpeed", 2.0f);
    }

    public void SpawnGroundImpact()
    {
        float direction = Mathf.Sign(transform.localScale.x);

        direction *= -1;

        GroundImpact impact = Instantiate(
            groundImpactPrefab,
            stabPoint.position,
            Quaternion.identity);

        impact.Initialize(direction);
    }

    public void GroundStabDash()
    {
        float dashImplitude = playerTransform.position.x - transform.position.x;

        
         float X = transform.position.x + Mathf.Sign(dashImplitude) * -1 * offsetX * 0.3f;
         X = Mathf.Clamp(X, boundX.x, boundX.y);
         transform.position = new Vector3(X, transform.position.y, transform.position.z);
        

        if(damageable.hpPercentage < 0.50)
        {

            X = transform.position.x + Mathf.Sign(dashImplitude) * -1 * offsetX * 0.2f;
            X = Mathf.Clamp(X, boundX.x, boundX.y); 
            transform.position = new Vector3(X, transform.position.y, transform.position.z);
        }
    }


    public void GroundStabEnd()
    {
        
        anim.SetFloat("GroundStabSpeed", 1.0f);
        SetIdle(1.0f);
    }


    // Summon particles like fire to chase the player else summon a big monster to play aside with the boss which is unrecommend
    private void StartSummon()
    {
        OnSpellCast = true;
        anim.SetTrigger("Summon");
    }

    private void Summon()
    {

    }

    private void SummonEnd()
    {
        SetIdle(0.5f);
    }

    private void SetIdle(float time)
    {
        state = BossState.Idle;
        idleMaxTime = time;
        if(damageable.hpPercentage <= 0.75)
        {
            idleMaxTime *= 0.8f;
        }
        else if (damageable.hpPercentage <= 0.5)
        {
            idleMaxTime *= 0.6f;
        }
        idleTime = 0.0f;
        OnSpellCast = false;
    }

    private void Dead()
    {

    }

    float bonusForNotUse = 3.0f;


    float stabBonus = 0.0f;
    float groundstabBonus = 0.0f;
    float SpecialAttackBonus = 0.0f;
    float SummonAttackBonus = 0.0f;


    private int WeightStab(float distance)
    {
        if(distance > offsetX * 1.5)
        {
            return INF;
        }

        if(distance > offsetX * 1.25)
        {
            return 30 + (int)stabBonus;
        }

        if(damageable.hpPercentage < 0.75)
        {
            return 30 + (int)stabBonus; // lower priority
        }

        return 50 + (int)stabBonus;
    }

    public void Died()
    {
        Destroy(this.gameObject);
    }
    private int WeightGroundStab(float distance)
    {

        
        if(distance > offsetX * 1.5)
        {
            return 50 + (int)groundstabBonus ;
        }

        if(distance > offsetX * 1.25)
        {
            return 30 + (int)groundstabBonus;
        }

        

        return 30 + (int)groundstabBonus;
    }

    private bool firstTimeAttackNormal = false;


    private int WeightSpecialAttackNormal(float distance)
    {
        if (damageable.hpPercentage > 0.9)
        {
            return INF;
        }

        if(firstTimeAttackNormal == false)
        {
            firstTimeAttackNormal = true;
            return INF - 1;
        }
        if (distance > offsetX * 1.5)
        {
            return 10 + (int)SpecialAttackBonus;
        }

        return 5 + (int)SpecialAttackBonus;
    }

    private bool firstTime = false;
    private bool frenzyOccur = false;
    private int WeightSpecialAttackFrenzy(float distance)
    {
        if(damageable.hpPercentage > 0.5)
        {
            return INF;
        }
       

        if (firstTime == false)
        {
            firstTime = true;
            return INF - 1; // surely commit
        }
        if(frenzyOccur == true)
        {
            frenzyOccur = false;
            return INF;
        }
        // rare
        return 5 + (int)SummonAttackBonus;
    }

    class AttackInfo
    {
        public SpellState state;
        public int weight;

        public AttackInfo(SpellState _state, int _weight)
        {
            state = _state;
            weight = _weight;
        }
    };


    private void SetUpAttack()
    {
        List<AttackInfo> availableAttack = new List<AttackInfo>();

        float distance = Mathf.Abs(playerTransform.position.x - transform.position.x);
        int weight = WeightStab(distance);

        int totalWeight = 0;
        

        
        if(weight != INF)
        {
            availableAttack.Add(new AttackInfo(SpellState.Stab, weight));
            totalWeight += weight;
        }

        
        weight = WeightGroundStab(distance);

        if (weight != INF)
        {
            totalWeight += weight;
            availableAttack.Add(new AttackInfo(SpellState.StabtoGround, totalWeight));
            
        }
        
        

        weight = WeightSpecialAttackNormal(distance);

        if(weight != INF)
        {
            totalWeight += weight;
            availableAttack.Add(new AttackInfo(SpellState.SpecialAbility, totalWeight));
        }
       
        weight = WeightSpecialAttackFrenzy(distance);
        if(weight != INF)
        {
            totalWeight += weight;
            availableAttack.Add(new AttackInfo(SpellState.Frenzy, totalWeight));
        }


        int choice = Random.Range(0, totalWeight);

        AttackInfo selectAttack = null;

        for(int i = 0; i < availableAttack.Count; i++)
        {
            if (choice <= availableAttack[i].weight)
            {
                selectAttack = availableAttack[i];
                break;
            }
        }


        
        if (selectAttack.state == SpellState.Stab)
        {
            StartStab();
            stabBonus = 0;
            groundstabBonus += bonusForNotUse;
            SpecialAttackBonus += bonusForNotUse;
            SummonAttackBonus += bonusForNotUse;
        }

        if (selectAttack.state == SpellState.StabtoGround)
        {


            StartGroundStab();
            stabBonus += bonusForNotUse;
            groundstabBonus = 0;
            SpecialAttackBonus += bonusForNotUse;
            SummonAttackBonus += bonusForNotUse;
        }

        if (selectAttack.state == SpellState.SpecialAbility)
        {
            stabBonus += bonusForNotUse;
            groundstabBonus += bonusForNotUse;
            SpecialAttackBonus = 0;
            SummonAttackBonus += bonusForNotUse;
            specialType = SpecialType.EarthWake;
            StartSpecialAbility();
        }

        if (selectAttack.state == SpellState.Frenzy)
        {
            stabBonus += bonusForNotUse;
            groundstabBonus += bonusForNotUse;
            SpecialAttackBonus = bonusForNotUse;
            SummonAttackBonus = 0;
            specialType = SpecialType.Frenzy;
            frenzyOccur = true;
            StartSpecialAbility();
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (!Released || CutSceneManager.Instance.IsInCutScene || (OnSpellCast && (state != BossState.DisapearSpawn && state != BossState.Stab ))) return;

        if (state == BossState.Chase)
        {
            Chase();
        }
        else if (state == BossState.Idle)
        {
            Idle();
        }
        else if (state == BossState.Dead)
        {
            Dead();
        }else if(state == BossState.DisapearSpawn)
        {
            SpecialUpUpdate();
        }else if(state == BossState.Stab)
        {
            StabUpdate();
        }
    }  

    private void FixedUpdate()
    {
        if (!Released) return;
    }

    public void OnHit(int damage, Vector2 hitKnockback)
    {
        effect.BloodEffect(hitKnockback.normalized);
        effect.Flash();

    }

    public void Release()
    {
        Released = true;
        anim.SetBool("Released", true);
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravityScale;
    }


    private bool animationDead = false;

    public bool isAlive()
    {
        return damageable.IsAlive || (animationDead == false);
    }

    public void AnimationDeadBoss()
    {
        animationDead = true;
    }

    public void AnimationFlip()
    {
        transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
    }


    

    public void DoneFalling()
    {
        if (CutSceneManager.Instance.IsInCutScene)
        {
            SetIdle(1.0f);
            return;
        }

        if(state == BossState.DisapearSpawn && SpecialType.Frenzy == specialType && startSpecialTime <= endSpecialTime)
        {
            StartSpecialAbility();
            return;
        }

        SetIdle(1.5f);
    }
}
