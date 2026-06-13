using UnityEngine;

/// <summary>
/// Legacy 技能物体基类：子弹、闪电、火焰等 Prefab 实例的攻击载体。
/// 伤害统一经 <see cref="DamageInfo"/> → <see cref="DamagePipeline"/> 结算，与新技能系统管线对齐。
/// </summary>
public class SkillObject_Base : MonoBehaviour, IAttackable
{
    #region 基础数值
    /// <summary>本物体造成的伤害值。</summary>
    protected float damageValue;
    /// <summary>攻击来源技能 ID（用于伤害归因）。</summary>
    protected string attackName;
    #endregion

    /// <summary>物体刚体组件。</summary>
    protected Rigidbody2D rb;

    [SerializeField] protected bool canMove;
    [SerializeField] protected float moveSpeed;

    /// <summary>初始化刚体引用并默认禁止移动。</summary>
    public virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        canMove = false;
    }

    /// <summary>每帧更新（子类可重写移动/生命周期逻辑）。</summary>
    protected virtual void Update() { }

    /// <summary>激活时允许移动。</summary>
    public virtual void OnEnable()
    {
        canMove = true;
    }

    /// <summary>禁用时停止移动并取消 Invoke 调度。</summary>
    public virtual void OnDisable()
    {
        canMove = false;
        CancelInvoke();
    }

    /// <summary>
    /// 配置攻击物体参数（方向、攻击信息与伤害）。
    /// </summary>
    /// <param name="moveDirection">移动方向（子类可使用）。</param>
    /// <param name="info">攻击信息。</param>
    /// <param name="damage">伤害值。</param>
    public virtual void SetupAttackObject(Vector2 moveDirection, AttackInfo info, float damage)
    {
        damageValue = damage;
        if (string.IsNullOrEmpty(attackName))
        {
            attackName = GameConstants.ConfigIds.SkillShoot;
        }
    }

    /// <summary>
    /// 对实体造成伤害（优先 IDamagable，否则走 DamagePipeline）。
    /// </summary>
    /// <param name="enemy">受击实体。</param>
    /// <param name="damage">伤害数值。</param>
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

    /// <summary>回收或重置物体状态（子类在回池前调用）。</summary>
    protected virtual void RecoverObjectStatus() { }
}
