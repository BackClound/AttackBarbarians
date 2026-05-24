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
    private sealed class ActiveEvent
    {
        public GameplayEventDataSO Data;
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

    public bool IsInitialized => isInitialized;
    public int ActiveEventCount => activeEvents.Count;

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

    private void OnMapLoaded(GameEventContext ctx)
    {
        if (!enableRandomEvents || configManager?.Database == null)
        {
            return;
        }

        TryTriggerEventsForType(GameplayEventTriggerType.OnMapLoad, waveIndex: 1);
        TryTriggerLinkedMapEvents();
    }

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

    private void RecomputeEventModifiers()
    {
        scratchEvents.Clear();
        for (int i = 0; i < activeEvents.Count; i++)
        {
            scratchEvents.Add(activeEvents[i].Data);
        }

        MapRuntimeContext.RecomputeEventModifiers(scratchEvents);
    }

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
