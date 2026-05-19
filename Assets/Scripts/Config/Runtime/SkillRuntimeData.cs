/// <summary>
/// 技能运行时数据：保存 configId、等级与缩放后的战斗参数。
/// </summary>
public sealed class SkillRuntimeData
{
    public string ConfigId { get; private set; }
    public SkillType SkillType { get; private set; }
    public int Level { get; private set; }
    public float Cooldown { get; private set; }
    public int MaxAttackCount { get; private set; }
    public int BulletsPerWave { get; private set; }
    public float CheckRadius { get; private set; }

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

    public void SetLevel(SkillDataSO source, int level)
    {
        Initialize(source, level);
    }

    private static int ClampLevel(SkillDataSO source, int level)
    {
        return UnityEngine.Mathf.Clamp(level < 1 ? 1 : level, 1, source.MaxLevel);
    }

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
