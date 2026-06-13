using UnityEngine;

/// <summary>
/// 战斗实体基类：缓存 Animator/Rigidbody2D/Collider2D，并提供统一伤害入口。
/// </summary>
/// <remarks>由 <see cref="Player"/>、<see cref="Enemy"/> 等子类继承并挂载到对应 Prefab 根节点。</remarks>
public class Entity : MonoBehaviour, IDamagable
{
    /// <summary>实体级状态机。</summary>
    public StateMachine stateMachine;
    /// <summary>子物体 Animator 组件。</summary>
    public Animator anim { get; private set; }
    /// <summary>2D 刚体组件。</summary>
    public Rigidbody2D rb;
    /// <summary>2D 碰撞体组件。</summary>
    public Collider2D coll;

    /// <summary>缓存子组件引用。</summary>
    public virtual void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
    }

    /// <summary>子类可覆写的启动钩子。</summary>
    public virtual void Start() { }

    /// <summary>动画播放完成时由 <see cref="EntityAnimatorTrigger"/> 调用。</summary>
    public virtual void OnAniamtorFinished() { }

    /// <summary>兼容 float 伤害入口，内部转换为 <see cref="DamageInfo"/>。</summary>
    /// <param name="damage">伤害数值。</param>
    public virtual void TakeDamage(float damage)
    {
        TakeDamage(DamageInfo.FromFloat(damage, gameObject));
    }

    /// <summary>统一伤害入口，委托 <see cref="DamagePipeline"/> 结算。</summary>
    /// <param name="info">伤害上下文。</param>
    /// <returns>结算结果。</returns>
    public virtual DamageResult TakeDamage(DamageInfo info)
    {
        DamageInfo resolved = info.Target != null ? info : info.WithTarget(gameObject);
        return DamagePipeline.ApplyToDamagable(this, resolved);
    }
}
