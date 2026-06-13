/// <summary>
/// 升级卡种类：技能专属、属性专属、通用技能、通用属性。
/// </summary>
public enum UpgradeCardCategory
{
    /// <summary>指定技能升级卡。</summary>
    Skill = 0,
    /// <summary>指定属性升级卡。</summary>
    Attribute = 1,
    /// <summary>通用技能卡（随机解析为具体技能）。</summary>
    GenericSkill = 2,
    /// <summary>通用属性卡（随机解析为具体属性）。</summary>
    GenericAttribute = 3,
}
