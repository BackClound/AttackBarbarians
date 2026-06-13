using UnityEngine;

/// <summary>
/// 实体状态基类：封装 Animator 布尔参数与物理更新的生命周期钩子。
/// </summary>
/// <remarks>由具体 Idle/Move/Attack 等状态子类继承，无需单独挂载。</remarks>
public class EntityState
{
    protected StateMachine stateMachine;
    protected string animName;
    protected bool isAnimFinished;
    protected Animator anim;
    protected Rigidbody2D rb;

    /// <summary>构造状态并绑定所属状态机与动画参数名。</summary>
    /// <param name="machine">宿主状态机。</param>
    /// <param name="animName">Animator 布尔参数名。</param>
    public EntityState(StateMachine machine, string animName)
    {
        this.animName = animName;
        this.stateMachine = machine;
    }

    /// <summary>进入状态时开启对应 Animator 布尔参数。</summary>
    public virtual void OnEnter()
    {
        anim?.SetBool(animName, true);
        isAnimFinished = false;
    }

    /// <summary>每帧逻辑更新。</summary>
    public virtual void OnUpdate() { }

    /// <summary>固定时间步物理更新。</summary>
    public virtual void OnFixedUpdate() { }

    /// <summary>离开状态时关闭 Animator 布尔参数。</summary>
    public virtual void OnExit()
    {
        if (anim != null)
        {
            anim.SetBool(animName, false);
        }
    }

    /// <summary>动画播放完成回调。</summary>
    public virtual void OnAnimFinished()
    {
        isAnimFinished = true;
    }

    /// <summary>动画攻击帧触发回调，子类实现具体出伤逻辑。</summary>
    public virtual void OnAnimAttackTrigger()
    {

    }

    /// <summary>按攻速倍率调整 Animator 播放速度。</summary>
    public virtual void ApplyAnimSpeedMulti() { }
}
