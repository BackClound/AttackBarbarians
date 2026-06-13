using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 局外技能解锁：按存档 <c>totalPlayTimeSeconds</c> 与解锁表判定，并在开局同步到 <see cref="SkillManager"/>。
/// 流水线位置：元进度评估 → 存档写入 → 开局 <see cref="ApplyUnlocksToPlayerSkillManager"/>。
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

    /// <summary>服务是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;

    /// <summary>订阅事件并解析解锁表。</summary>
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

    /// <summary>每帧 Tick（本服务无逐帧逻辑）。</summary>
    /// <param name="deltaTime">帧间隔秒数。</param>
    public void Tick(float deltaTime) { }

    /// <summary>取消订阅并标记未初始化。</summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
        isInitialized = false;
    }

    /// <summary>
    /// 累计游玩秒数（存档 + 当前局）。
    /// </summary>
    /// <returns>总游玩秒数。</returns>
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

    /// <summary>
    /// 是否已在元进度中解锁（存档或时长达标）。
    /// </summary>
    /// <param name="skillConfigId">技能配置 ID。</param>
    /// <returns>是否已元解锁。</returns>
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

    /// <summary>
    /// 评估时长解锁并写入存档，返回本帧新解锁数量。
    /// </summary>
    /// <param name="newlyUnlockedBuffer">可选，接收新解锁技能 ID 列表。</param>
    /// <returns>本帧新解锁技能数量。</returns>
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

    /// <summary>
    /// 将存档中已解锁技能同步到场景内 <see cref="SkillManager"/>。
    /// </summary>
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

    /// <summary>
    /// 获取技能元解锁所需累计游玩秒数。
    /// </summary>
    /// <param name="skillConfigId">技能配置 ID。</param>
    /// <returns>所需秒数；未知技能返回 <see cref="long.MaxValue"/>。</returns>
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

    /// <summary>对局开始时刷新元解锁并同步到玩家。</summary>
    /// <param name="ctx">游戏开始事件上下文。</param>
    private void OnGameStarted(GameEventContext ctx)
    {
        RefreshMetaUnlocks();
        ApplyUnlocksToPlayerSkillManager();
    }

    /// <summary>解析解锁表（Override → ConfigDatabase → Resources）。</summary>
    /// <returns>解锁表资产；可能为 null。</returns>
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

    /// <summary>
    /// 遍历全部解锁规则（配置表或内置 Fallback）。
    /// </summary>
    /// <param name="visitor">访问器：(技能ID, 所需秒数, 是否默认解锁)。</param>
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

    /// <summary>解析场景中的 <see cref="PlayerSkillManager"/>。</summary>
    /// <returns>玩家技能管理器；未找到时返回 null。</returns>
    private static PlayerSkillManager ResolvePlayerSkillManager()
    {
        if (Player.HasInstance && Player.Instance.skillManager != null)
        {
            return Player.Instance.skillManager;
        }

        return FindFirstObjectByType<PlayerSkillManager>();
    }
}
