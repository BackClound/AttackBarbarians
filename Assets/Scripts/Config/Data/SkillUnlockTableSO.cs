using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能累计时长解锁表；由 <see cref="SkillUnlockService"/> 在开局与结算时评估。
/// </summary>
[CreateAssetMenu(fileName = "SkillUnlockTable", menuName = "Attack Barbarians/Config/Skill Unlock Table")]
public class SkillUnlockTableSO : ScriptableObject
{
    [SerializeField] private List<SkillUnlockEntryConfig> entries = new List<SkillUnlockEntryConfig>(8);

    public IReadOnlyList<SkillUnlockEntryConfig> Entries => entries;

    public bool TryGetRequiredSeconds(string skillConfigId, out long seconds)
    {
        seconds = 0;
        if (string.IsNullOrWhiteSpace(skillConfigId) || entries == null)
        {
            return false;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            SkillUnlockEntryConfig entry = entries[i];
            if (entry != null && entry.SkillConfigId == skillConfigId)
            {
                seconds = entry.RequiredPlayTimeSeconds;
                return true;
            }
        }

        return false;
    }

    public void CollectValidationErrors(ConfigValidationResult result)
    {
        if (entries == null || entries.Count == 0)
        {
            result.AddWarning(name, "解锁表为空，将使用内置默认阈值。");
            return;
        }

        var seen = new HashSet<string>();
        for (int i = 0; i < entries.Count; i++)
        {
            SkillUnlockEntryConfig entry = entries[i];
            if (entry == null)
            {
                result.AddError(name, $"entries[{i}] 为空。");
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.SkillConfigId))
            {
                result.AddError(name, $"entries[{i}] 未配置 skillConfigId。");
                continue;
            }

            if (!seen.Add(entry.SkillConfigId))
            {
                result.AddError(name, $"重复的 skillConfigId: {entry.SkillConfigId}");
            }
        }
    }
}
