using UnityEngine;

/// <summary>
/// 具有攻击能力的物体，包括子弹，闪电，火焰等prefab创建的物体
/// 
/// TODO 怎么获取entity所具有的攻击，防御等方面的数据
/// </summary>
public class AttackObject : MonoBehaviour, IAttackable
{
    #region 基础数值
    private float damageValue;
    private string attackName;
    #endregion

    #region 目标信息
    [SerializeField] private Transform target;
    [SerializeField] private bool isStartAttacking;
    private Vector2 originalPosition;
    #endregion
    [SerializeField] private bool canMove;
    [SerializeField] private float moveSpeed;
    private void Awake()
    {
        canMove = false;
        originalPosition = transform.position;
    }

    private void Update()
    {
        if (canMove)
        {
            transform.position = Vector2.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, target.position) < 0.3f)
            {
                RecoverObjectStatus();
            }
        }
    }

    private void OnEnable()
    {
        canMove = true;
    }

    private void OnDisable()
    {
        canMove = false;
    }

    public void SetupAttackObject(Enemy target, AttackInfo info, float damage)
    {
        this.target = target.transform;
        canMove = true;
        //target will Take damagevalue, and update the realHp
        damageValue = damage;
        //将会对enemy进行攻击，对enemy的真实血量进行预更新
        target.enemy_Health.WillReduceHp(damage);
    }

    public void DoDamage(Entity_Stats stats, float damage)
    {
        stats.ReduceHp(damage);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        //获取接触到的可被攻击的敌人
        if (collision != null)
        {
            var enemy = collision.GetComponent<Enemy>();
            DoDamage(enemy.enemy_Health.entity_Stats, damageValue);
        }
    }

    public void RecoverObjectStatus()
    {
        gameObject.SetActive(false);
        canMove = false;
        transform.position = originalPosition;
    }
}


