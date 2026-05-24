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

    public string SkillConfigId => skillConfigId;
    public long RequiredPlayTimeSeconds => System.Math.Max(0L, requiredPlayTimeSeconds);
    public bool UnlockedByDefault => unlockedByDefault;
}
