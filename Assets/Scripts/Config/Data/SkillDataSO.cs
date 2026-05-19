using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能配置：类型、等级曲线与每级数值。
/// </summary>
[CreateAssetMenu(fileName = "SkillData", menuName = "Attack Barbarians/Config/Skill Data")]
public class SkillDataSO : ConfigDataBase
{
    [Header("Skill")]
    [SerializeField] private SkillType skillType;
    [SerializeField] private int maxLevel = 10;
    [SerializeField] private GameObject skillPrefab;

    [Header("Combat")]
    [SerializeField] private float baseCooldown = 4f;
    [SerializeField] private int maxAttackCount = 10;
    [SerializeField] private int bulletsPerWave = 3;
    [SerializeField] private float checkRadius = 25f;

    [Header("Level Scaling")]
    [SerializeField] private List<SkillLevelEntryConfig> levelEntries = new List<SkillLevelEntryConfig>();

    public SkillType SkillType => skillType;
    public int MaxLevel => Mathf.Max(1, maxLevel);
    public GameObject SkillPrefab => skillPrefab;
    public float BaseCooldown => Mathf.Max(0.05f, baseCooldown);
    public int MaxAttackCount => Mathf.Max(1, maxAttackCount);
    public int BulletsPerWave => Mathf.Max(1, bulletsPerWave);
    public float CheckRadius => Mathf.Max(1f, checkRadius);
    public IReadOnlyList<SkillLevelEntryConfig> LevelEntries => levelEntries;

    public bool TryGetLevelEntry(int level, out SkillLevelEntryConfig entry)
    {
        entry = null;
        if (levelEntries == null || levelEntries.Count == 0)
        {
            return false;
        }

        int clamped = Mathf.Clamp(level, 1, levelEntries.Count);
        entry = levelEntries[clamped - 1];
        return entry != null;
    }

    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (maxLevel < 1)
        {
            result.AddError(name, "maxLevel 不能小于 1。");
        }

        if (levelEntries == null || levelEntries.Count == 0)
        {
            result.AddWarning(name, "未配置 levelEntries，将使用默认缩放。");
            return;
        }

        if (levelEntries.Count < maxLevel)
        {
            result.AddWarning(name, $"levelEntries 数量 ({levelEntries.Count}) 少于 maxLevel ({maxLevel})。");
        }

        for (int i = 0; i < levelEntries.Count; i++)
        {
            SkillLevelEntryConfig levelEntry = levelEntries[i];
            if (levelEntry == null)
            {
                result.AddError(name, $"levelEntries[{i}] 为空。");
                continue;
            }

            if (levelEntry.Level < 1)
            {
                result.AddError(name, $"levelEntries[{i}] 等级非法: {levelEntry.Level}");
            }
        }
    }
}

/// <summary>技能单级数值与缩放。</summary>
[System.Serializable]
public class SkillLevelEntryConfig
{
    [SerializeField] private int level = 1;
    [SerializeField] private SkillScaleData scaleData = new SkillScaleData();

    public int Level => level;
    public SkillScaleData ScaleData => scaleData;
}
