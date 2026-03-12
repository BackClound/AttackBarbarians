using UnityEngine;

//在level等级中，设置两列/三列/两排/三排/换弹时发射散弹或者从左到右的一排子弹
public class SkillShoot : SkillBase
{
    private Player player;
    private Player_Stats player_Stats;
    //TODO 这是Player当前的武器，可以是发射子弹/激光/或者其他的
    [SerializeField] private GameObject shootPrefab;
    [SerializeField] private Transform bulletSpownPoint;
    [Header("基础攻击数值")]
    [SerializeField] private Transform[] bullets;
    [SerializeField] private int maxAttackCount = 10;
    [SerializeField] private int currentAttackCount;
    [SerializeField] private float attackInterval;
    [SerializeField] private int currentAttackTime = 0;

    [Header("多发弹道信息")]
    [SerializeField] private int shootLine;
    [SerializeField] private int shootWave;

    [Header("多波次攻击")]
    private float spaceBetweenBulletLine;
    private float spaceBetweenBulletWave;

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
    }

    protected override void Update()
    {

    }
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
    public void ActivateAttackEnemy()
    {
        var effectiveEnemys = player.playerCombatManager.effectiveEnemys;

        //没有发现敌人或者开始coolDown
        if (effectiveEnemys.Count <= 0 || currentAttackCount >= maxAttackCount) return;

        //分配攻击次数和敌人，当有新的enemy并且bullet不为空的时候，为新的敌人分配为可攻击的子弹
        foreach (Enemy enemy in effectiveEnemys)
        {
            Debug.Log("ActivateAttackEnemy check enemy state = " + enemy.CanBeDamage());

            if (enemy.CanBeDamage())
            {
                bullets[currentAttackCount].GetComponent<AttackObject>().SetupAttackObject(enemy.transform, null, 10f);
                ActivateBulletOrNot(bullets[currentAttackCount].gameObject, true);
                currentAttackCount++;
                return;
            }
        }
    }

}
