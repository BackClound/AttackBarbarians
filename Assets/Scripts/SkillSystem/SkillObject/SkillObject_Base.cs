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

    public virtual void OnEnable()
    {
        canMove = true;
    }

    public virtual void OnDisable()
    {
        canMove = false;
        CancelInvoke();
    }

    public virtual void SetupAttackObject(Vector2 moveDirection, AttackInfo info, float damage)
    {
        damageValue = damage;
        if (string.IsNullOrEmpty(attackName))
        {
            attackName = GameConstants.ConfigIds.SkillShoot;
        }
    }

    public void DoDamage(Entity enemy, float damage)
    {
        if (enemy == null)
        {
            return;
        }

        object source = Player.HasInstance ? Player.Instance : (object)gameObject;
        DamageInfo info = DamageInfo.Create(
            source,
            enemy.gameObject,
            damage,
            skillId: attackName);

        if (enemy is IDamagable damagable)
        {
            damagable.TakeDamage(info);
            return;
        }

        DamagePipeline.Apply(info);
    }


    protected virtual void RecoverObjectStatus() { }
}


