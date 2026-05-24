using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 局外技能解锁：按存档 <c>totalPlayTimeSeconds</c> 与解锁表判定，并在开局同步到 <see cref="SkillManager"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。</para>
/// <para><b>推荐挂载对象：</b><c>GameSystems</c>，由 <see cref="GameBootstrapper"/> 注册。</para>
/// <para><b>获取方式：</b><c>ServiceLocator.Get&lt;SkillUnlockService&gt;()</c></para>
/// </remarks>
public class SkillUnlockService : MonoBehaviour, IGameSystem
{
    private struct FallbackUnlockRule
    {
        public string SkillId;
        public long RequiredSeconds;
        public bool UnlockedByDefault;
    }

    private static readonly FallbackUnlockRule[] FallbackRules =
    {
        new() { SkillId = GameConstants.ConfigIds.SkillShoot, RequiredSeconds = 0, UnlockedByDefault = true },
        new() { SkillId = GameConstants.ConfigIds.SkillLightning, RequiredSeconds = 600, UnlockedByDefault = false },
        new() { SkillId = GameConstants.ConfigIds.SkillHeal, RequiredSeconds = 900, UnlockedByDefault = false },
        new() { SkillId = GameConstants.ConfigIds.SkillThunder, RequiredSeconds = 1800, UnlockedByDefault = false },
        new() { SkillId = GameConstants.ConfigIds.SkillFireRain, RequiredSeconds = 3600, UnlockedByDefault = false },
        new() { SkillId = GameConstants.ConfigIds.SkillIce, RequiredSeconds = 5400, UnlockedByDefault = false },
        new() { SkillId = GameConstants.ConfigIds.SkillWaterWave, RequiredSeconds = 7200, UnlockedByDefault = false },
    };

    [SerializeField] private SkillUnlockTableSO unlockTableOverride;

    private ConfigManager configManager;
    private SaveManager saveManager;
    private RunSessionTracker runSessionTracker;
    private SkillUnlockTableSO unlockTable;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        ServiceLocator.TryGet(out configManager);
        ServiceLocator.TryGet(out saveManager);
        ServiceLocator.TryGet(out runSessionTracker);
        unlockTable = ResolveUnlockTable();
        GameEvents.SubscribeGameStarted(OnGameStarted);
        isInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
        isInitialized = false;
    }

    /// <summary>累计游玩秒数（存档 + 当前局）。</summary>
    public long GetTotalPlayTimeSeconds()
    {
        long total = 0;
        if (saveManager?.Current?.statistics != null)
        {
            total = saveManager.Current.statistics.totalPlayTimeSeconds;
        }

        if (runSessionTracker != null)
        {
            total += (long)runSessionTracker.SessionDurationSeconds;
        }

        return total;
    }

    /// <summary>是否已在元进度中解锁（存档或时长达标）。</summary>
    public bool IsMetaUnlocked(string skillConfigId)
    {
        if (string.IsNullOrWhiteSpace(skillConfigId))
        {
            return false;
        }

        if (skillConfigId == GameConstants.ConfigIds.SkillShoot)
        {
            return true;
        }

        if (saveManager?.Current != null && saveManager.Current.GetSkillLevel(skillConfigId) > 0)
        {
            return true;
        }

        return GetTotalPlayTimeSeconds() >= GetRequiredSeconds(skillConfigId);
    }

    /// <summary>评估时长解锁并写入存档，返回本帧新解锁数量。</summary>
    public int RefreshMetaUnlocks(List<string> newlyUnlockedBuffer = null)
    {
        newlyUnlockedBuffer?.Clear();
        SaveData save = saveManager?.Current;
        if (save == null)
        {
            return 0;
        }

        long playTime = GetTotalPlayTimeSeconds();
        int count = 0;
        IterateRules((skillId, requiredSeconds, unlockedByDefault) =>
        {
            bool shouldUnlock = unlockedByDefault || playTime >= requiredSeconds;
            if (!shouldUnlock || save.GetSkillLevel(skillId) > 0)
            {
                return;
            }

            save.SetSkillLevel(skillId, 1);
            saveManager.MarkDirty();
            count++;
            newlyUnlockedBuffer?.Add(skillId);
            GameEvents.RaiseSkillUnlocked(this, skillId);
        });

        return count;
    }

    /// <summary>将存档中已解锁技能同步到场景内 <see cref="SkillManager"/>。</summary>
    public void ApplyUnlocksToPlayerSkillManager()
    {
        PlayerSkillManager playerSkills = ResolvePlayerSkillManager();
        if (playerSkills?.SkillManager == null)
        {
            return;
        }

        SkillManager manager = playerSkills.SkillManager;
        SaveData save = saveManager?.Current;
        IterateRules((skillId, requiredSeconds, unlockedByDefault) =>
        {
            bool unlocked = unlockedByDefault ||
                            (save != null && save.GetSkillLevel(skillId) > 0) ||
                            GetTotalPlayTimeSeconds() >= requiredSeconds;
            if (!unlocked)
            {
                return;
            }

            int level = save != null ? Mathf.Max(1, save.GetSkillLevel(skillId)) : 1;
            manager.UnlockSkill(skillId, level);
        });
    }

    public long GetRequiredSeconds(string skillConfigId)
    {
        if (unlockTable != null && unlockTable.TryGetRequiredSeconds(skillConfigId, out long seconds))
        {
            return seconds;
        }

        for (int i = 0; i < FallbackRules.Length; i++)
        {
            if (FallbackRules[i].SkillId == skillConfigId)
            {
                return FallbackRules[i].RequiredSeconds;
            }
        }

        return long.MaxValue;
    }

    private void OnGameStarted(GameEventContext ctx)
    {
        RefreshMetaUnlocks();
        ApplyUnlocksToPlayerSkillManager();
    }

    private SkillUnlockTableSO ResolveUnlockTable()
    {
        if (unlockTableOverride != null)
        {
            return unlockTableOverride;
        }

        if (configManager?.Database != null && configManager.Database.SkillUnlockTable != null)
        {
            return configManager.Database.SkillUnlockTable;
        }

        return Resources.Load<SkillUnlockTableSO>(GameConstants.ResourcePaths.SkillUnlockTable);
    }

    private void IterateRules(System.Action<string, long, bool> visitor)
    {
        if (unlockTable != null && unlockTable.Entries != null && unlockTable.Entries.Count > 0)
        {
            for (int i = 0; i < unlockTable.Entries.Count; i++)
            {
                SkillUnlockEntryConfig entry = unlockTable.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.SkillConfigId))
                {
                    continue;
                }

                visitor(entry.SkillConfigId, entry.RequiredPlayTimeSeconds, entry.UnlockedByDefault);
            }

            return;
        }

        for (int i = 0; i < FallbackRules.Length; i++)
        {
            FallbackUnlockRule rule = FallbackRules[i];
            visitor(rule.SkillId, rule.RequiredSeconds, rule.UnlockedByDefault);
        }
    }

    private static PlayerSkillManager ResolvePlayerSkillManager()
    {
        if (Player.HasInstance && Player.Instance.skillManager != null)
        {
            return Player.Instance.skillManager;
        }

        return FindFirstObjectByType<PlayerSkillManager>();
    }
}
