using UnityEngine;

/// <summary>
/// 单条技能解锁规则：累计游玩秒数达到阈值后永久解锁（写入存档）。
/// </summary>
[System.Serializable]
public class SkillUnlockEntryConfig
{
    [SerializeField] private string skillConfigId;
    [SerializeField] private long requiredPlayTimeSeconds;
    [SerializeField] private bool unlockedByDefault;

    /// <summary>技能配置唯一标识。</summary>
    public string SkillConfigId => skillConfigId;

    /// <summary>解锁所需的累计游玩秒数。</summary>
    public long RequiredPlayTimeSeconds => System.Math.Max(0L, requiredPlayTimeSeconds);

    /// <summary>是否默认已解锁（如新存档即开放）。</summary>
    public bool UnlockedByDefault => unlockedByDefault;
}
