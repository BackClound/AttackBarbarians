/// <summary>
/// 技能运行时数据：保存 configId、等级与缩放后的战斗参数。
/// </summary>
public sealed class SkillRuntimeData
{
    /// <summary>技能配置唯一标识。</summary>
    public string ConfigId { get; private set; }

    /// <summary>技能类型。</summary>
    public SkillType SkillType { get; private set; }

    /// <summary>当前技能等级。</summary>
    public int Level { get; private set; }

    /// <summary>冷却时间（秒，已按等级缩放）。</summary>
    public float Cooldown { get; private set; }

    /// <summary>最大攻击次数。</summary>
    public int MaxAttackCount { get; private set; }

    /// <summary>每波发射子弹数。</summary>
    public int BulletsPerWave { get; private set; }

    /// <summary>索敌检测半径。</summary>
    public float CheckRadius { get; private set; }

    /// <summary>
    /// 从技能配置资产初始化运行时数据，并按等级应用缩放。
    /// </summary>
    /// <param name="source">来源技能配置；为 null 时不执行任何操作。</param>
    /// <param name="level">技能等级。</param>
    public void Initialize(SkillDataSO source, int level)
    {
        if (source == null)
        {
            return;
        }

        ConfigId = source.ConfigId;
        SkillType = source.SkillType;
        Level = ClampLevel(source, level);
        Cooldown = source.BaseCooldown;
        MaxAttackCount = source.MaxAttackCount;
        BulletsPerWave = source.BulletsPerWave;
        CheckRadius = source.CheckRadius;

        if (source.TryGetLevelEntry(Level, out SkillLevelEntryConfig entry) && entry.ScaleData != null)
        {
            ApplyScale(entry.ScaleData);
        }
    }

    /// <summary>
    /// 重新设置技能等级并刷新所有战斗参数。
    /// </summary>
    /// <param name="source">来源技能配置。</param>
    /// <param name="level">新的技能等级。</param>
    public void SetLevel(SkillDataSO source, int level)
    {
        Initialize(source, level);
    }

    /// <summary>
    /// 将技能等级限制在配置允许的有效范围内。
    /// </summary>
    /// <param name="source">来源技能配置。</param>
    /// <param name="level">请求的等级。</param>
    /// <returns>限制在 [1, MaxLevel] 范围内的等级。</returns>
    private static int ClampLevel(SkillDataSO source, int level)
    {
        return UnityEngine.Mathf.Clamp(level < 1 ? 1 : level, 1, source.MaxLevel);
    }

    /// <summary>
    /// 根据等级缩放数据调整冷却等战斗参数。
    /// </summary>
    /// <param name="scale">等级对应的缩放配置。</param>
    private void ApplyScale(SkillScaleData scale)
    {
        if (scale.coolDownScaleMulti > 0f)
        {
            Cooldown *= 1f - UnityEngine.Mathf.Clamp(scale.coolDownScaleMulti, 0f, 0.5f);
        }

        if (scale.attackSpeedScaleMulti > 0f)
        {
            Cooldown /= scale.attackSpeedScaleMulti;
        }
    }
}
