using UnityEngine;

/// <summary>
/// 具有攻击能力的物体，包括子弹，闪电，火焰等prefab创建的物体
/// 
/// TODO 怎么获取entity所具有的攻击，防御等方面的数据
/// </summary>
public class SkillObject_Base : MonoBehaviour, IAttackable
{
    #region 基础数值
    protected float damageValue;
    protected string attackName;
    #endregion

    protected Rigidbody2D rb;

    [SerializeField] protected bool canMove;
    [SerializeField] protected float moveSpeed;
    public virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        canMove = false;
    }

    protected virtual void Update() { }

    private void OnEnable()
    {
        canMove = true;
        Invoke("RecoverObjectStatus", 4f);
    }

    private void OnDisable()
    {
        canMove = false;
        CancelInvoke();
    }

    public virtual void SetupAttackObject(Enemy target, AttackInfo info, float damage) { }

    public void DoDamage(Entity enemy, float damage)
    {
        Debug.Log("Attack Object start do damage " + damage);
        enemy.TakeDamage(damage);
    }


    protected virtual void RecoverObjectStatus() { }
}


