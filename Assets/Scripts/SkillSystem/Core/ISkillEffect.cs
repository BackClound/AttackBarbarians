/// <summary>
/// 技能效果执行接口；新增技能只需实现本接口并在 <see cref="SkillEffectFactory"/> 注册。
/// 流水线位置：<see cref="SkillManager"/> 冷却就绪 → <see cref="TryAutoCast"/> → 伤害/投射物 → <see cref="DamagePipeline"/>。
/// </summary>
public interface ISkillEffect
{
    /// <summary>本效果对应的技能类型。</summary>
    SkillType SkillType { get; }

    /// <summary>
    /// 自动释放技能：冷却就绪且有目标时由 <see cref="SkillManager"/> 调用。
    /// </summary>
    /// <param name="context">技能释放上下文。</param>
    /// <param name="runtime">技能运行时数据。</param>
    /// <returns>是否成功施放。</returns>
    bool TryAutoCast(SkillContext context, SkillRuntime runtime);

    /// <summary>
    /// 外部触发施法（如射击动画帧、手动调用）。
    /// </summary>
    /// <param name="context">技能释放上下文。</param>
    /// <param name="runtime">技能运行时数据。</param>
    void OnExternalCast(SkillContext context, SkillRuntime runtime);
}
