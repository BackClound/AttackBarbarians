using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 从 <see cref="GameConfig"/> 应用试玩/调试配置：开局 Buff 等。
/// 技能解锁覆盖由 <see cref="SkillUnlockService"/> 在同步解锁时处理。
/// </summary>
/// <remarks>纯静态工具，无需挂载。</remarks>
public static class PlaytestBootstrap
{
    private const int StartupBuffRetryFrames = 90;

    private static bool isSubscribed;
    private static bool startupBuffsAppliedForRun;

    /// <summary>订阅开局事件以应用试玩配置。</summary>
    /// <param name="gameConfig">游戏全局配置。</param>
    public static void Initialize(GameConfig gameConfig)
    {
        if (!isSubscribed)
        {
            GameEvents.SubscribeGameStarted(OnGameStarted);
            isSubscribed = true;
        }

        if (gameConfig != null && gameConfig.EnableRuntimeLogs)
        {
            Debug.Log(
                $"[PlaytestBootstrap] SkillUnlockOverrides={gameConfig.EnableSkillUnlockOverrides} " +
                $"count={gameConfig.SkillUnlockOverrides.Count}, " +
                $"StartupBuffs={gameConfig.EnableStartupBuffs} count={gameConfig.StartupBuffs.Count}");
        }
    }

    /// <summary>新一局开始时重置开局 Buff 应用标记。</summary>
    /// <param name="ctx">游戏开始事件上下文。</param>
    private static void OnGameStarted(GameEventContext ctx)
    {
        startupBuffsAppliedForRun = false;
    }

    /// <summary>
    /// 在技能解锁同步之后尝试施加开局 Buff（幂等，每局只成功应用一次）。
    /// </summary>
    /// <param name="gameConfig">可选；为空时从 <see cref="ConfigManager"/> 读取。</param>
    /// <returns>是否已完成施加（含「未启用」或「列表为空」）。</returns>
    public static bool TryApplyStartupBuffs(GameConfig gameConfig = null)
    {
        if (startupBuffsAppliedForRun)
        {
            return true;
        }

        if (gameConfig == null && ServiceLocator.TryGet(out ConfigManager configManager))
        {
            gameConfig = configManager.GameConfig;
        }

        if (!GameConfigPlaytestSettings.IsStartupBuffEnabled(gameConfig))
        {
            startupBuffsAppliedForRun = true;
            return true;
        }

        if (ResolveBuffManager() == null)
        {
            if (GameBootstrapper.Instance != null)
            {
                GameBootstrapper.Instance.StartCoroutine(ApplyStartupBuffsDeferred(gameConfig));
            }

            return false;
        }

        return ApplyStartupBuffs(gameConfig);
    }

    private static IEnumerator ApplyStartupBuffsDeferred(GameConfig gameConfig)
    {
        for (int i = 0; i < StartupBuffRetryFrames; i++)
        {
            yield return null;
            if (TryApplyStartupBuffs(gameConfig))
            {
                yield break;
            }
        }

        if (gameConfig != null && gameConfig.EnableRuntimeLogs)
        {
            Debug.LogWarning(
                $"[PlaytestBootstrap] 在 {StartupBuffRetryFrames} 帧内未找到 BuffManager，开局 Buff 未施加。");
        }
    }

    /// <summary>为玩家施加 <see cref="GameConfig"/> 中配置的开局 Buff。</summary>
    /// <param name="gameConfig">游戏全局配置。</param>
    /// <returns>是否完成处理（含无有效条目时的警告输出）。</returns>
    public static bool ApplyStartupBuffs(GameConfig gameConfig)
    {
        if (!GameConfigPlaytestSettings.IsStartupBuffEnabled(gameConfig))
        {
            startupBuffsAppliedForRun = true;
            return true;
        }

        BuffManager buffManager = ResolveBuffManager();
        if (buffManager == null)
        {
            return false;
        }

        IReadOnlyList<GameConfigStartupBuffEntry> entries = gameConfig.StartupBuffs;
        int appliedCount = 0;
        int nullCount = 0;
        ServiceLocator.TryGet(out ConfigManager configManager);
        for (int i = 0; i < entries.Count; i++)
        {
            GameConfigStartupBuffEntry entry = entries[i];
            BuffDataSO buff = ResolveBuffData(entry, configManager);
            if (buff == null)
            {
                nullCount++;
                continue;
            }

            buffManager.ApplyBuff(buff, entry.Stacks);
            appliedCount++;
            if (gameConfig.EnableRuntimeLogs)
            {
                Debug.Log(
                    $"[PlaytestBootstrap] Applied startup buff: {buff.ConfigId} x{entry.Stacks}");
            }
        }

        if (appliedCount == 0)
        {
            Debug.LogWarning(
                "[PlaytestBootstrap] Enable Startup Buffs 已勾选，但没有有效的 Buff 引用。" +
                "请在 GameConfig → Playtest → Startup Buffs 中拖入 BuffDataSO，或填写 Buff Config Id。" +
                (nullCount > 0 ? $" 当前有 {nullCount} 个无法解析的条目。" : " 当前列表为空。"));
        }

        startupBuffsAppliedForRun = true;
        return true;
    }

    /// <summary>解析场景中的 <see cref="BuffManager"/>。</summary>
    private static BuffManager ResolveBuffManager()
    {
        if (Player.HasInstance && Player.Instance.skillManager != null)
        {
            BuffManager manager = Player.Instance.skillManager.BuffManager;
            if (manager != null)
            {
                return manager;
            }
        }

        PlayerSkillManager playerSkills = Player.HasInstance
            ? Player.Instance.skillManager
            : Object.FindFirstObjectByType<PlayerSkillManager>();
        return playerSkills != null ? playerSkills.BuffManager : Object.FindFirstObjectByType<BuffManager>();
    }

    private static BuffDataSO ResolveBuffData(GameConfigStartupBuffEntry entry, ConfigManager configManager)
    {
        if (entry == null)
        {
            return null;
        }

        if (entry.Buff != null)
        {
            return entry.Buff;
        }

        string configId = entry.BuffConfigId;
        if (string.IsNullOrWhiteSpace(configId))
        {
            return null;
        }

        if (configManager != null && configManager.TryGetBuff(configId, out BuffDataSO fromDatabase))
        {
            return fromDatabase;
        }

        BuffDataSO[] resourcesBuffs = Resources.LoadAll<BuffDataSO>("Config/Buff");
        for (int i = 0; i < resourcesBuffs.Length; i++)
        {
            BuffDataSO candidate = resourcesBuffs[i];
            if (candidate != null && candidate.ConfigId == configId)
            {
                return candidate;
            }
        }

        return null;
    }
}
