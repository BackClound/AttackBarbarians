using UnityEngine;

/// <summary>
/// 具有攻击能力的物体，包括子弹，闪电，火焰等prefab创建的物体
/// 
/// TODO 怎么获取entity所具有的攻击，防御等方面的数据
/// </summary>
public class AttackObject : MonoBehaviour, IAttackable
{
    #region 基础数值
    private Stat damageValue;
    private string attackName;
    #endregion

    #region 目标信息
    [SerializeField] private Transform target;
    [SerializeField] private bool isStartAttacking;
    #endregion
    [SerializeField] private bool canMove;
    [SerializeField] private float moveSpeed;
    private void Awake()
    {
        canMove = false;
    }

    private void Update()
    {
        if (canMove)
        {
            transform.position = Vector2.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
        }
    }

    public void SetupAttackObject(Transform target, AttackInfo info, float damage)
    {
        this.target = target;
        canMove = true;
        //初始化target ， damagevalue, 
        // damageValue.SetBaseValue(damage);
    }

    public void DoDamage(float damage)
    {
        //攻击敌人
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        //获取接触到的可被攻击的敌人
    }
}


