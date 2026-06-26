/// <summary>
/// 实体动画驱动：统一 Unity Animator 与 Spine 等后端的播放接口。
/// </summary>
public interface IEntityAnimationDriver
{
    /// <summary>逻辑状态进入时同步表现层。</summary>
    void OnStateEnter(EntityState state, string animParam);

    /// <summary>逻辑状态退出时同步表现层。</summary>
    void OnStateExit(EntityState state, string animParam);

    /// <summary>写入浮点 Animator 参数（如攻速倍率）。</summary>
    void SetFloat(string paramName, float value);

    /// <summary>播放一次性脉冲动画（如射击），由 <see cref="OnAnimationEventFinished"/> 复位。</summary>
    void PlayPulse(string paramName);

    /// <summary>动画事件通知单次脉冲结束。</summary>
    void OnAnimationEventFinished();

    /// <summary>对象池回收或重置时清理动画状态。</summary>
    void ResetDriver();
}
