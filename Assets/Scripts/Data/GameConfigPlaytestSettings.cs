using System.Collections.Generic;
using UnityEngine;

/// <summary>技能解锁覆盖状态（运行前在 <see cref="GameConfig"/> 中配置）。</summary>
public enum GameConfigSkillUnlockOverrideState
{
    /// <summary>强制解锁（可指定等级）。</summary>
    ForceUnlock = 0,

    /// <summary>强制锁定（仅本局运行时生效）。</summary>
    ForceLock = 1,
}

/// <summary>单条技能解锁覆盖：在元进度解锁之后强制解锁或锁定。</summary>
[System.Serializable]
public class GameConfigSkillUnlockOverride
{
    [Tooltip("拖入技能配置资产；若为空则使用 Skill Config Id。")]
    [SerializeField] private SkillDataSO skill;

    [SerializeField] private string skillConfigId;

    [SerializeField] private GameConfigSkillUnlockOverrideState state = GameConfigSkillUnlockOverrideState.ForceUnlock;

    [Min(1)]
    [SerializeField] private int level = 1;

    /// <summary>覆盖状态。</summary>
    public GameConfigSkillUnlockOverrideState State => state;

    /// <summary>强制解锁时的等级。</summary>
    public int Level => Mathf.Max(1, level);

    /// <summary>解析技能配置 ID。</summary>
    public string ResolveSkillConfigId()
    {
        if (skill != null && !string.IsNullOrWhiteSpace(skill.ConfigId))
        {
            return skill.ConfigId;
        }

        return string.IsNullOrWhiteSpace(skillConfigId) ? null : skillConfigId.Trim();
    }
}

/// <summary>开局自动施加的 Buff 条目。</summary>
[System.Serializable]
public class GameConfigStartupBuffEntry
{
    [SerializeField] private BuffDataSO buff;

    [Tooltip("Buff 资产引用丢失时，按 configId 从 ConfigManager / Resources 解析。")]
    [SerializeField] private string buffConfigId;

    [Min(1)]
    [SerializeField] private int stacks = 1;

    /// <summary>Buff 配置。</summary>
    public BuffDataSO Buff => buff;

    /// <summary>Buff 配置 Id（备用解析）。</summary>
    public string BuffConfigId => string.IsNullOrWhiteSpace(buffConfigId) ? null : buffConfigId.Trim();

    /// <summary>堆叠层数。</summary>
    public int Stacks => Mathf.Max(1, stacks);
}

/// <summary><see cref="GameConfig"/> 调试/试玩配置的只读访问器。</summary>
public static class GameConfigPlaytestSettings
{
    /// <summary>是否启用技能解锁覆盖。</summary>
    public static bool IsSkillUnlockOverrideEnabled(GameConfig config) =>
        config != null && config.EnableSkillUnlockOverrides;

    /// <summary>技能解锁覆盖列表。</summary>
    public static IReadOnlyList<GameConfigSkillUnlockOverride> GetSkillUnlockOverrides(GameConfig config) =>
        config != null ? config.SkillUnlockOverrides : System.Array.Empty<GameConfigSkillUnlockOverride>();

    /// <summary>是否启用开局 Buff。</summary>
    public static bool IsStartupBuffEnabled(GameConfig config) =>
        config != null && config.EnableStartupBuffs;

    /// <summary>开局 Buff 列表。</summary>
    public static IReadOnlyList<GameConfigStartupBuffEntry> GetStartupBuffs(GameConfig config) =>
        config != null ? config.StartupBuffs : System.Array.Empty<GameConfigStartupBuffEntry>();

    /// <summary>尝试获取指定技能的覆盖状态。</summary>
    public static bool TryGetSkillUnlockOverride(
        GameConfig config,
        string skillConfigId,
        out GameConfigSkillUnlockOverrideState state,
        out int level)
    {
        state = GameConfigSkillUnlockOverrideState.ForceUnlock;
        level = 1;

        if (!IsSkillUnlockOverrideEnabled(config) || string.IsNullOrWhiteSpace(skillConfigId))
        {
            return false;
        }

        IReadOnlyList<GameConfigSkillUnlockOverride> overrides = config.SkillUnlockOverrides;
        for (int i = 0; i < overrides.Count; i++)
        {
            GameConfigSkillUnlockOverride entry = overrides[i];
            if (entry == null)
            {
                continue;
            }

            string id = entry.ResolveSkillConfigId();
            if (id != skillConfigId)
            {
                continue;
            }

            state = entry.State;
            level = entry.Level;
            return true;
        }

        return false;
    }
}
