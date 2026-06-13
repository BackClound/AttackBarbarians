using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能累计时长解锁表；由 <see cref="SkillUnlockService"/> 在开局与结算时评估。
/// </summary>
/// <remarks>
/// <para><b>创建：</b>Attack Barbarians → Config → Skill Unlock Table。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/Skill/SkillUnlockTable.asset</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "SkillUnlockTable", menuName = "Attack Barbarians/Config/Skill Unlock Table")]
public class SkillUnlockTableSO : ScriptableObject
{
    [SerializeField] private List<SkillUnlockEntryConfig> entries = new List<SkillUnlockEntryConfig>(8);

    /// <summary>所有技能解锁规则条目。</summary>
    public IReadOnlyList<SkillUnlockEntryConfig> Entries => entries;

    /// <summary>
    /// 查询指定技能解锁所需的累计游玩秒数。
    /// </summary>
    /// <param name="skillConfigId">技能配置唯一标识。</param>
    /// <param name="seconds">所需累计游玩秒数；未配置时为 0。</param>
    /// <returns>找到对应条目时返回 true，否则返回 false。</returns>
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

    /// <summary>
    /// 收集技能解锁表的校验错误与警告。
    /// </summary>
    /// <param name="result">校验结果容器，用于写入错误与警告信息。</param>
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
