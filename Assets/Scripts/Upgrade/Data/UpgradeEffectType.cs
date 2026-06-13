/// <summary>
/// 升级选项效果类型，由 <see cref="UpgradeApplicator"/> 分发给各系统。
/// </summary>
public enum UpgradeEffectType
{
    /// <summary>属性 Buff 或直接属性修正。</summary>
    StatBuff = 0,
    /// <summary>技能 Buff 强化。</summary>
    SkillBuff = 1,
    /// <summary>解锁技能。</summary>
    SkillUnlock = 2,
    /// <summary>提升技能等级。</summary>
    SkillLevelUp = 3,
    /// <summary>金币资源奖励。</summary>
    ResourceGold = 4,
    /// <summary>钻石资源奖励。</summary>
    ResourceDiamond = 5,
    /// <summary>武器强化（技能 Buff 的一种）。</summary>
    WeaponEnhance = 6,
}
