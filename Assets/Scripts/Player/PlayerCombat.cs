using System.Collections;
using UnityEngine;

/// <summary>
/// TODO是否需要重命名为PlayerCombatManager
/// 这里只分配对应的enemy
/// </summary>
public class PlayerCombat : EntityCombat
{
    private Player player;
    [SerializeField] protected Player_Stats entity_Stats;
    [SerializeField] private Transform bulletSpownPoint;
    //TODO 这是Player当前的武器，可以是发射子弹/激光/或者其他的
    [SerializeField] private GameObject bulletPre;
    private Transform[] bullets;
    private int maxBulletCount;
    [SerializeField] private float attackInterval;
    [SerializeField] private int currentAttackTime = 0;

    protected override void Awake()
    {
        player = GetComponent<Player>();
        entity_Stats = player.player_Stats;
        maxBulletCount = Mathf.FloorToInt(entity_Stats.maxBulletCount.GetFinalValue());
        if (bullets == null || bullets.Length < maxBulletCount)
        {
            bullets = new Transform[maxBulletCount];
            InitialBulletList();
        }

    }

    private void FixedUpdate()
    {
        //在攻击完成之后，再次检测攻击
        if (!isAttacking)
        {
            CheckEnemyInRadius();

        }

        if (canAttack && !isAttacking)
        {
            //切换player状态为PlayerShootState
            StartCoroutine(AttackEnemyWithWeaponCo());

        }

        //当 coolDown == false && isAttacking == false, 切换Player状态为PlayerIdleState

    }

    protected override void CheckEnemyInRadius()
    {
        effectiveEnemys.Clear();
        canAttack = false;
        //获取离得最近的Enemy
        Collider2D[] colliders = Physics2D.OverlapCircleAll(checkEnemy.position, checkEnemyDistance, enemyLayer);
        Debug.Log("CheckEnemyInRadius colliders count = " + colliders.Length);
        foreach (var coll in colliders)
        {
            Debug.Log("CheckEnemyInRadius current coll = " + coll + " coll Tag = " + coll.gameObject.gameObject);
            if (coll != null && coll.gameObject.CompareTag(enemyTag))
            {
                Enemy enemy = coll.GetComponent<Enemy>();
                effectiveEnemys.Add(enemy);
                canAttack = true;
            }
        }
        if (effectiveEnemys.Count <= 0 || currentAttackTime >= maxBulletCount) return;

        //分配攻击次数和敌人，当有新的enemy并且bullet不为空的时候，为新的敌人分配为可攻击的子弹
        for (int i = 0; i < maxBulletCount; i++)
        {
            int index = Random.Range(0, effectiveEnemys.Count);
            bullets[i].GetComponent<AttackObject>().SetupAttackObject(effectiveEnemys[index].transform, null, 10f);
        }
    }

    private IEnumerator AttackEnemyWithWeaponCo()
    {
        isAttacking = true;
        while (currentAttackTime < maxBulletCount)
        {
            //TODO  同步子弹发射的时机和Player attack动画的同步机制
            AttackEnemyWithWeapon(bullets[currentAttackTime].gameObject);
            yield return new WaitForSeconds(attackInterval);
            currentAttackTime++;
        }
        isAttacking = false;
    }

    protected override void AttackEnemyWithWeapon(GameObject bullet)
    {
        bullet.SetActive(true);
    }

    private void InitialBulletList()
    {
        for (int i = 0; i < maxBulletCount; i++)
        {
            GameObject bullet = CreateBulletPre();
            bullet.SetActive(false);
            bullets[i] = bullet.transform;
        }
    }

    private GameObject CreateBulletPre()
    {
        GameObject bullet = Instantiate(bulletPre, bulletSpownPoint.position, Quaternion.identity);
        return bullet;
    }

}
