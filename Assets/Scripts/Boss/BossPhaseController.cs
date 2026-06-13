using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss 阶段管理：按血量比例或时间推进，并应用阶段属性修正。
/// </summary>
/// <remarks>纯逻辑组件，由 <see cref="BossController"/> 驱动，无需单独挂载。</remarks>
public sealed class BossPhaseController
{
    private BossDataSO bossData;
    private Enemy_Health health;
    private Entity_Stats entityStats;
    private GameObject bossObject;
    private BossPhaseTransitionMode transitionMode;
    private float aliveSeconds;
    private int currentPhaseIndex;
    private bool isActive;
    private readonly StatRuntimeSnapshot phaseSnapshot = new StatRuntimeSnapshot();
    private readonly List<float> timeThresholds = new List<float>(4);

    /// <summary>当前阶段索引（从 0 开始）。</summary>
    public int CurrentPhaseIndex => currentPhaseIndex;
    /// <summary>总阶段数。</summary>
    public int PhaseCount => bossData != null ? bossData.PhaseCount : 1;
    /// <summary>阶段控制器是否处于激活状态。</summary>
    public bool IsActive => isActive;

    /// <summary>
    /// 绑定 Boss 数据与宿主组件并初始化阶段状态。
    /// </summary>
    /// <param name="data">Boss 配置。</param>
    /// <param name="enemyHealth">Boss 血量组件。</param>
    /// <param name="stats">Boss 属性组件。</param>
    /// <param name="owner">Boss GameObject。</param>
    public void Initialize(
        BossDataSO data,
        Enemy_Health enemyHealth,
        Entity_Stats stats,
        GameObject owner)
    {
        bossData = data;
        health = enemyHealth;
        entityStats = stats;
        bossObject = owner;
        transitionMode = data != null ? data.PhaseTransitionMode : BossPhaseTransitionMode.HealthRatio;
        aliveSeconds = 0f;
        currentPhaseIndex = 0;
        isActive = data != null && enemyHealth != null && stats != null;
        timeThresholds.Clear();

        if (!isActive || data == null)
        {
            return;
        }

        IReadOnlyList<float> thresholds = data.PhaseTimeThresholds;
        if (thresholds != null)
        {
            for (int i = 0; i < thresholds.Count; i++)
            {
                timeThresholds.Add(Mathf.Max(0f, thresholds[i]));
            }
        }
    }

    /// <summary>每帧推进存活时间并尝试切换阶段。</summary>
    /// <param name="deltaTime">帧间隔时间（秒）。</param>
    public void Tick(float deltaTime)
    {
        if (!isActive || bossData == null)
        {
            return;
        }

        aliveSeconds += deltaTime;

        if (transitionMode == BossPhaseTransitionMode.ElapsedTime)
        {
            TryAdvanceByTime();
            return;
        }

        TryAdvanceByHealth();
    }

    /// <summary>释放引用并重置激活状态。</summary>
    public void Shutdown()
    {
        isActive = false;
        bossData = null;
        health = null;
        entityStats = null;
        bossObject = null;
    }

    /// <summary>按当前血量比例尝试推进阶段。</summary>
    private void TryAdvanceByHealth()
    {
        float maxHp = health.MaxHp;
        if (maxHp <= 0f)
        {
            return;
        }

        float ratio = health.CurrentHp / maxHp;
        IReadOnlyList<float> thresholds = bossData.PhaseHpThresholds;
        if (thresholds == null || thresholds.Count == 0)
        {
            return;
        }

        int targetPhase = 0;
        for (int i = 0; i < thresholds.Count; i++)
        {
            if (ratio <= thresholds[i])
            {
                targetPhase = i + 1;
            }
        }

        int maxPhase = bossData.PhaseCount - 1;
        targetPhase = Mathf.Min(targetPhase, maxPhase);
        TrySetPhase(targetPhase, ratio);
    }

    /// <summary>按存活时间尝试推进阶段。</summary>
    private void TryAdvanceByTime()
    {
        if (timeThresholds.Count == 0)
        {
            return;
        }

        int targetPhase = 0;
        for (int i = 0; i < timeThresholds.Count; i++)
        {
            if (aliveSeconds >= timeThresholds[i])
            {
                targetPhase = i + 1;
            }
        }

        int maxPhase = bossData.PhaseCount - 1;
        targetPhase = Mathf.Min(targetPhase, maxPhase);

        float ratio = health != null && health.MaxHp > 0f ? health.CurrentHp / health.MaxHp : 1f;
        TrySetPhase(targetPhase, ratio);
    }

    /// <summary>切换到新阶段并广播阶段变更事件。</summary>
    /// <param name="newPhaseIndex">目标阶段索引。</param>
    /// <param name="hpRatio">当前血量比例。</param>
    private void TrySetPhase(int newPhaseIndex, float hpRatio)
    {
        if (newPhaseIndex <= currentPhaseIndex)
        {
            return;
        }

        int previous = currentPhaseIndex;
        currentPhaseIndex = newPhaseIndex;
        ApplyPhaseModifiers(newPhaseIndex);

        GameEvents.RaiseBossPhaseChanged(bossObject, new BossPhaseChangedEventArgs(
            bossData.ConfigId,
            bossObject,
            previous,
            newPhaseIndex,
            hpRatio));

        GameEvents.RaiseAudioPlaySfx(bossObject, GameConstants.AudioIds.SfxBossPhase);
    }

    /// <summary>应用指定阶段的属性修正到 Boss。</summary>
    /// <param name="phaseIndex">阶段索引。</param>
    private void ApplyPhaseModifiers(int phaseIndex)
    {
        if (entityStats == null || bossData == null)
        {
            return;
        }

        IReadOnlyList<StatModifierConfig> modifiers = bossData.GetPhaseModifiers(phaseIndex);
        if (modifiers == null || modifiers.Count == 0)
        {
            return;
        }

        phaseSnapshot.CopyFromEntityStats(entityStats);
        phaseSnapshot.ApplyModifiers(modifiers);
        ConfigStatBridge.ApplyToEntityStats(phaseSnapshot, entityStats);

        if (health != null)
        {
            float newMax = entityStats.GetMaxHp();
            if (newMax > health.CurrentHp)
            {
                health.RaiseHp(newMax - health.CurrentHp);
            }
        }
    }
}
