/// <summary>
/// 技能类型枚举，与 <see cref="SkillDataSO"/>、<see cref="SkillBuffProfile"/> 目标技能对应。
/// 保留 0=Shoot 以兼容既有 <c>SkillData_Shoot.asset</c> 序列化。
/// </summary>
public enum SkillType
{
    Shoot = 0,
    FireRain = 1,
    Ice = 2,
    Lightning = 3,
    Thunder = 4,
    WaterWave = 5,
    Heal = 6,
    None = 99,
}
