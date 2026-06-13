/// <summary>
/// 技能类型枚举，与 <see cref="SkillDataSO"/>、<see cref="SkillBuffProfile"/> 目标技能对应。
/// 保留 0=Shoot 以兼容既有 <c>SkillData_Shoot.asset</c> 序列化。
/// </summary>
public enum SkillType
{
    /// <summary>射击：直线投射物，单/多弹道。</summary>
    Shoot = 0,
    /// <summary>火雨：随机区域多 Tick 伤害。</summary>
    FireRain = 1,
    /// <summary>冰霜：扇形投射物，穿透并冰冻。</summary>
    Ice = 2,
    /// <summary>闪电：链式命中，可选麻痹与末端爆炸。</summary>
    Lightning = 3,
    /// <summary>落雷：随机落点 AoE，可选持续区域。</summary>
    Thunder = 4,
    /// <summary>水浪：方向波，减速敌人。</summary>
    WaterWave = 5,
    /// <summary>恢复：被动回血与定时治疗 Buff。</summary>
    Heal = 6,
    /// <summary>无效/占位类型。</summary>
    None = 99,
}
