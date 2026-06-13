using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 局内随机事件管理器：按配置触发、计时并在结束时撤销效果。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 <c>GameSystems</c> 上。</para>
/// <para><b>获取方式：</b><c>ServiceLocator.Get&lt;GameplayEventManager&gt;()</c>。</para>
/// </remarks>
public class GameplayEventManager : MonoBehaviour, IGameSystem
{
    /// <summary>当前激活事件的运行时记录。</summary>
    private sealed class ActiveEvent
    {
        /// <summary>事件配置。</summary>
        public GameplayEventDataSO Data;
        /// <summary>剩余持续时间（秒）。</summary>
        public float RemainingSeconds;
    }

    [SerializeField] private bool enableRandomEvents = true;

    private const float RandomDuringWaveCheckIntervalSeconds = 8f;

    private readonly List<ActiveEvent> activeEvents = new List<ActiveEvent>(4);
    private readonly List<GameplayEventDataSO> scratchEvents = new List<GameplayEventDataSO>(4);

    private ContentRegistry contentRegistry;
    private MapManager mapManager;
    private ConfigManager configManager;
    private int currentWaveIndex = 1;
    private bool waveActive;
    private float randomDuringWaveTimer;
    private bool isInitialized;

    /// <summary>管理器是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;
    /// <summary>当前激活事件数量。</summary>
    public int ActiveEventCount => activeEvents.Count;

    /// <summary>订阅波次/地图事件并完成初始化。</summary>
    public void Initialize()
    {
        contentRegistry = ServiceLocator.TryGet(out ContentRegistry registry) ? registry : null;
        mapManager = ServiceLocator.TryGet(out MapManager mm) ? mm : null;
        configManager = ServiceLocator.TryGet(out ConfigManager cm) ? cm : null;

        GameEvents.SubscribeMapLoaded(OnMapLoaded);
        GameEvents.SubscribeWaveStarted(OnWaveStarted);
        GameEvents.SubscribeWaveCompleted(OnWaveCompleted);

        isInitialized = true;

        if (mapManager != null && mapManager.IsMapLoaded)
        {
            TryTriggerEventsForType(GameplayEventTriggerType.OnMapLoad, waveIndex: 1);
            TryTriggerLinkedMapEvents();
        }
    }

    /// <summary>驱动波中随机检测与事件倒计时。</summary>
    /// <param name="deltaTime">帧间隔时间（秒）。</param>
    public void Tick(float deltaTime)
    {
        if (!isInitialized || !enableRandomEvents)
        {
            return;
        }

        TickRandomDuringWave(deltaTime);

        if (activeEvents.Count == 0)
        {
            return;
        }

        for (int i = activeEvents.Count - 1; i >= 0; i--)
        {
            ActiveEvent active = activeEvents[i];
            if (active.Data.DurationSeconds <= 0f)
            {
                continue;
            }

            active.RemainingSeconds -= deltaTime;
            if (active.RemainingSeconds <= 0f)
            {
                EndEventAt(i);
            }
        }
    }

    /// <summary>取消订阅、清空激活事件并重置修正。</summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeMapLoaded(OnMapLoaded);
        GameEvents.UnsubscribeWaveStarted(OnWaveStarted);
        GameEvents.UnsubscribeWaveCompleted(OnWaveCompleted);

        activeEvents.Clear();
        MapRuntimeContext.ResetEventModifiers();
        currentWaveIndex = 1;
        waveActive = false;
        randomDuringWaveTimer = 0f;
        isInitialized = false;
    }

    /// <summary>地图加载完成时触发 OnMapLoad 与关联事件。</summary>
    /// <param name="ctx">事件上下文。</param>
    private void OnMapLoaded(GameEventContext ctx)
    {
        if (!enableRandomEvents || configManager?.Database == null)
        {
            return;
        }

        TryTriggerEventsForType(GameplayEventTriggerType.OnMapLoad, waveIndex: 1);
        TryTriggerLinkedMapEvents();
    }

    /// <summary>波次开始时更新索引并尝试触发 OnWaveStarted 事件。</summary>
    /// <param name="ctx">事件上下文。</param>
    private void OnWaveStarted(GameEventContext ctx)
    {
        if (ctx.Payload is not WaveEventArgs args)
        {
            return;
        }

        currentWaveIndex = args.WaveIndex;
        waveActive = true;
        randomDuringWaveTimer = RandomDuringWaveCheckIntervalSeconds;

        if (!enableRandomEvents)
        {
            return;
        }

        TryTriggerEventsForType(GameplayEventTriggerType.OnWaveStarted, args.WaveIndex);
    }

    /// <summary>波次完成时触发 OnWaveCompleted 并结束标记为波次结束的事件。</summary>
    /// <param name="ctx">事件上下文。</param>
    private void OnWaveCompleted(GameEventContext ctx)
    {
        if (ctx.Payload is not WaveEventArgs args)
        {
            return;
        }

        waveActive = false;

        if (!enableRandomEvents)
        {
            return;
        }

        TryTriggerEventsForType(GameplayEventTriggerType.OnWaveCompleted, args.WaveIndex);

        for (int i = activeEvents.Count - 1; i >= 0; i--)
        {
            if (activeEvents[i].Data.EndsOnWaveComplete)
            {
                EndEventAt(i);
            }
        }
    }

    /// <summary>波次进行中按间隔尝试 RandomDuringWave 触发。</summary>
    /// <param name="deltaTime">帧间隔时间（秒）。</param>
    private void TickRandomDuringWave(float deltaTime)
    {
        if (!waveActive)
        {
            return;
        }

        randomDuringWaveTimer -= deltaTime;
        if (randomDuringWaveTimer > 0f)
        {
            return;
        }

        randomDuringWaveTimer = RandomDuringWaveCheckIntervalSeconds;
        TryTriggerEventsForType(GameplayEventTriggerType.RandomDuringWave, currentWaveIndex);
    }

    /// <summary>触发当前地图绑定的局内事件。</summary>
    private void TryTriggerLinkedMapEvents()
    {
        if (mapManager == null || !mapManager.IsMapLoaded)
        {
            return;
        }

        IReadOnlyList<string> linkedIds = mapManager.CurrentMap.LinkedGameplayEventIds;
        if (linkedIds == null || linkedIds.Count == 0)
        {
            return;
        }

        for (int i = 0; i < linkedIds.Count; i++)
        {
            string eventId = linkedIds[i];
            if (contentRegistry != null && contentRegistry.TryGetGameplayEvent(eventId, out GameplayEventDataSO eventData))
            {
                TryStartEvent(eventData, waveIndex: 1);
            }
        }
    }

    /// <summary>按触发类型与波次范围筛选并尝试启动事件。</summary>
    /// <param name="triggerType">触发时机。</param>
    /// <param name="waveIndex">当前波次序号。</param>
    private void TryTriggerEventsForType(GameplayEventTriggerType triggerType, int waveIndex)
    {
        if (configManager?.Database?.GameplayEvents == null)
        {
            return;
        }

        IReadOnlyList<GameplayEventDataSO> events = configManager.Database.GameplayEvents;
        for (int i = 0; i < events.Count; i++)
        {
            GameplayEventDataSO eventData = events[i];
            if (eventData == null || eventData.Trigger.TriggerType != triggerType)
            {
                continue;
            }

            GameplayEventTriggerConfig trigger = eventData.Trigger;
            if (waveIndex < trigger.MinWaveIndex || waveIndex > trigger.MaxWaveIndex)
            {
                continue;
            }

            if (Random.value > trigger.TriggerChance)
            {
                continue;
            }

            TryStartEvent(eventData, waveIndex);
        }
    }

    /// <summary>启动单条事件：应用即时效果、加入激活列表并广播。</summary>
    /// <param name="eventData">事件配置。</param>
    /// <param name="waveIndex">当前波次序号。</param>
    private void TryStartEvent(GameplayEventDataSO eventData, int waveIndex)
    {
        if (eventData == null || IsEventActive(eventData.ConfigId))
        {
            return;
        }

        ApplyInstantEffects(eventData);

        var active = new ActiveEvent
        {
            Data = eventData,
            RemainingSeconds = eventData.DurationSeconds
        };
        activeEvents.Add(active);
        RecomputeEventModifiers();

        GameEvents.RaiseGameplayEventStarted(this, new GameplayEventArgs(
            eventData.ConfigId,
            waveIndex,
            eventData.DurationSeconds));
    }

    /// <summary>应用无需持续计时的即时效果（如玩家 Buff）。</summary>
    /// <param name="eventData">事件配置。</param>
    private void ApplyInstantEffects(GameplayEventDataSO eventData)
    {
        IReadOnlyList<GameplayEventEffectConfig> effects = eventData.Effects;
        if (effects == null)
        {
            return;
        }

        for (int i = 0; i < effects.Count; i++)
        {
            GameplayEventEffectConfig effect = effects[i];
            if (effect.EffectType != GameplayEventEffectType.ApplyPlayerBuff)
            {
                continue;
            }

            ApplyPlayerBuff(effect.StringParam);
        }
    }

    /// <summary>为玩家施加指定 Buff。</summary>
    /// <param name="buffConfigId">Buff 配置 Id。</param>
    private void ApplyPlayerBuff(string buffConfigId)
    {
        if (string.IsNullOrWhiteSpace(buffConfigId) || contentRegistry == null)
        {
            return;
        }

        if (!contentRegistry.TryGetBuff(buffConfigId, out BuffDataSO buffData))
        {
            Debug.LogWarning($"[GameplayEventManager] Buff 配置缺失 configId={buffConfigId}");
            return;
        }

        if (Player.HasInstance && Player.Instance.skillManager != null)
        {
            Player.Instance.skillManager.ApplyBuff(buffData, 1);
        }
    }

    /// <summary>结束指定索引的激活事件并重新计算修正。</summary>
    /// <param name="index">激活列表索引。</param>
    private void EndEventAt(int index)
    {
        if (index < 0 || index >= activeEvents.Count)
        {
            return;
        }

        GameplayEventDataSO ended = activeEvents[index].Data;
        activeEvents.RemoveAt(index);
        RecomputeEventModifiers();

        if (ended != null)
        {
            GameEvents.RaiseGameplayEventEnded(this, new GameplayEventArgs(ended.ConfigId, 0, 0f));
        }
    }

    /// <summary>根据当前激活事件重算 <see cref="MapRuntimeContext"/> 修正。</summary>
    private void RecomputeEventModifiers()
    {
        scratchEvents.Clear();
        for (int i = 0; i < activeEvents.Count; i++)
        {
            scratchEvents.Add(activeEvents[i].Data);
        }

        MapRuntimeContext.RecomputeEventModifiers(scratchEvents);
    }

    /// <summary>检查指定 configId 的事件是否已在激活中。</summary>
    /// <param name="configId">事件配置 Id。</param>
    /// <returns>已激活返回 true，否则返回 false。</returns>
    private bool IsEventActive(string configId)
    {
        for (int i = 0; i < activeEvents.Count; i++)
        {
            if (activeEvents[i].Data != null && activeEvents[i].Data.ConfigId == configId)
            {
                return true;
            }
        }

        return false;
    }
}
