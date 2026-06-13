using UnityEngine;

/// <summary>
/// 技能等级运行时数据容器，供 Legacy <see cref="SkillBase"/> 升级流程使用。
/// </summary>
public class SkillLevelData
{
    /// <summary>当前技能等级。</summary>
    public int skillLevel;
    /// <summary>技能类型。</summary>
    public SkillType skillType;
    /// <summary>等级对应的属性缩放系数。</summary>
    public SkillScaleData skillScaleData;
}
