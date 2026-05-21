/// <summary>
/// 技能效果执行接口；新增技能只需实现本接口并在 <see cref="SkillManager"/> 注册。
/// </summary>
public interface ISkillEffect
{
    SkillType SkillType { get; }

    /// <summary>自动释放技能：冷却就绪且有目标时调用。</summary>
    bool TryAutoCast(SkillContext context, SkillRuntime runtime);

    /// <summary>射击动画帧等外部触发（仅射击等需要）。</summary>
    void OnExternalCast(SkillContext context, SkillRuntime runtime);
}
