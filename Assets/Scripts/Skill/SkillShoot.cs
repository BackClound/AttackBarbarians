using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//在level等级中，设置两列/三列/两排/三排/换弹时发射散弹或者从左到右的一排子弹
public class SkillShoot : SkillBase
{
    private Player player;
    //TODO 这是Player当前的武器，可以是发射子弹/激光/或者其他的
    [SerializeField] private GameObject shootPrefab;
    [SerializeField] private Transform bulletSpownPoint;
    [Header("基础攻击数值")]
    //TODO 是否需要设置两个列表，一个控制available bullets，一个控制unAvailable bullets
    [SerializeField] private Transform[] bullets;
    [SerializeField] private int maxAttackCount = 10;
    [SerializeField] private int currentAttackCount;
    [SerializeField] private float attackInterval;
    [SerializeField] private float coolDownTimer = 0;
    [SerializeField] private float cooldownThreshold = 4;
    public float shootSpeedAnimMulti { get; private set; }


    [Header("Check Enemy info")]
    [SerializeField] protected float maxCheckDistance = 25;
    [SerializeField] protected Transform checkPosition;
    [SerializeField] protected LayerMask enemyLayer;
    [SerializeField] protected string enemyTag;
    [SerializeField] public List<Enemy> effectiveEnemys;



    [Header("多发弹道信息")]
    [SerializeField] private int shootLine;
    [SerializeField] private int shootWave;

    [Header("多波次攻击")]
    private float spaceBetweenBulletLine;
    private float spaceBetweenBulletWave;

    [SerializeField] private bool hasEffectiveEnemy = false;
    [SerializeField] private bool isCoolDown;

    protected override void Awake()
    {
        base.Awake();
        player = GetComponentInParent<Player>();
        // player_Stats = player.player_Stats;
        // maxAttackCount = Mathf.FloorToInt(player_Stats.maxBulletCount.GetFinalValue());
        if (bullets == null || bullets.Length < maxAttackCount)
        {
            bullets = new Transform[maxAttackCount];
            InitialBulletList();
        }
        coolDownTimer = cooldownThreshold;
        shootSpeedAnimMulti = .3f;
    }

    protected override void Update()
    {
        //处于未攻击状态， 实时检测enemy
        if (!hasEffectiveEnemy)
        {
            CheckEnemyInRadiusWithSorted();
        }
    }

    private IEnumerator StartCoolDownCo()
    {
        yield return new WaitForSeconds(coolDownTimer);
        isCoolDown = false;
        currentAttackCount = 0;
    }

    public bool CanUseShootSkill() => hasEffectiveEnemy && !isCoolDown;

    private void InitialBulletList()
    {
        for (int i = 0; i < maxAttackCount; i++)
        {
            GameObject bullet = CreateBulletPre();
            bullet.SetActive(false);
            bullets[i] = bullet.transform;
        }
    }

    private GameObject CreateBulletPre()
    {
        GameObject bullet = Instantiate(shootPrefab, bulletSpownPoint.position, Quaternion.identity);
        return bullet;
    }

    public void ActivateBulletOrNot(GameObject bullet, bool isActivite)
    {
        bullet.SetActive(isActivite);
    }

    /// <summary>
    /// PlayerShootState在动画结束的时候会调用该方法，激活Bullet
    /// </summary>
    public void ActivateOneShootAttack()
    {

        //1. 检查是否在cooldown， cooldown时直接return
        if (isCoolDown) return;

        //2. effective enemy为 null， 搜索附近敌人并进行排序，对最近的敌人进行攻击
        if (!hasEffectiveEnemy)
        {
            CheckEnemyInRadiusWithSorted();
            if (!hasEffectiveEnemy) return;
        }

        //3. 激活bullet进行攻击
        //分配攻击次数和敌人，当有新的enemy并且bullet不为空的时候，为新的敌人分配为可攻击的子弹
        foreach (Enemy enemy in effectiveEnemys)
        {
            // Debug.Log("ActivateAttackEnemy check enemy state = " + enemy.enemy_Health.CanBeDamage() + ", currentAttackCount = " + currentAttackCount);
            if (enemy.enemy_Health.CanBeDamage() && currentAttackCount < maxAttackCount)
            {
                bullets[currentAttackCount].GetComponent<AttackObject>().SetupAttackObject(enemy, null, 10f);
                bullets[currentAttackCount].gameObject.SetActive(true);
                // ActivateBulletOrNot(bullets[currentAttackCount].gameObject, true);
                currentAttackCount++;
                //退出循环，继续执行后面逻辑
                break;
            }
        }

        //4. 检测是否cool down， 如果需要cool down，更新状态 isCoolDown值
        if (currentAttackCount >= maxAttackCount)
        {
            isCoolDown = true;
            StartCoroutine(StartCoolDownCo());
        }

        //5. 攻击结束后，检测effective enemy
        CheckEnemyIsAvailable();

        //6. 当没有敌人或者技能coolDown
        if (effectiveEnemys.Count <= 0 || currentAttackCount >= maxAttackCount)
        {
            player.playerCombatManager.UpdateAttackStatus(false);
            player.stateMachine.ChangeState(player.idleState);
            return;
        }
    }

    /// <summary>
    /// 每次敌人之后，扫描最近的敌人
    /// 检测敌人，根据距离对enemy进行排序
    /// 获取enemy的血量信息，将该enemy作为Target分配给Bullet作为待攻击目标
    /// </summary>
    public void CheckEnemyInRadiusWithSorted()
    {
        effectiveEnemys.Clear();

        //获取离得最近的Enemy
        Vector2 startPosition = checkPosition.position;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(startPosition, maxCheckDistance, enemyLayer);
        // Debug.Log("CheckEnemyInRadius colliders count = " + colliders.Length);
        //对检测到的敌人进行排序
        System.Array.Sort(colliders, (a, b) =>
       {
           float sqrDistanceA = (startPosition - (Vector2)a.transform.position).sqrMagnitude;
           float sqrDistanceB = (startPosition - (Vector2)b.transform.position).sqrMagnitude;
           return sqrDistanceA.CompareTo(sqrDistanceB);

       });
        foreach (var coll in colliders)
        {
            Enemy enemy = coll.GetComponent<Enemy>();
            if (enemy != null && enemy.enemy_Health.CanBeDamage())
            {
                effectiveEnemys.Add(enemy);
            }
        }
        if (effectiveEnemys.Count > 0)
        {
            hasEffectiveEnemy = true;
        }
    }

    //应该在攻击结束之后，调用该方法，更新当前是否还有effective的敌人,移除available的敌人，更新hasEffectiveEnemy的值
    public void CheckEnemyIsAvailable()
    {
        effectiveEnemys.RemoveAll(enemy => !enemy.enemy_Health.CanBeDamage());
        //effective enemy是否为null，不为null， 直接return
        if (effectiveEnemys.Count > 0)
        {
            hasEffectiveEnemy = true;
            return;
        }
        //检测effective enemy == null， 搜索附近是否存在effective enemy，更新列表和hasEffectiveEnemy值
        hasEffectiveEnemy = false;
    }

}
