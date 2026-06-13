using System.Collections.Generic;

/// <summary>
/// 当前地图与局内事件对波次/刷怪的运行时修正（无挂载，由 <see cref="MapManager"/> 与 <see cref="GameplayEventManager"/> 写入）。
/// </summary>
public static class MapRuntimeContext
{
    private static MapDataSO currentMap;
    private static float mapEnemyStatMultiplier = 1f;
    private static float mapSpawnIntervalMultiplier = 1f;
    private static float mapMaxSpawnCountMultiplier = 1f;
    private static float eventEnemyStatMultiplier = 1f;
    private static float eventSpawnIntervalMultiplier = 1f;
    private static float eventRewardMultiplier = 1f;
    private static int pauseSpawnsCount;

    /// <summary>当前地图配置。</summary>
    public static MapDataSO CurrentMap => currentMap;
    /// <summary>敌人属性综合倍率（地图 × 事件）。</summary>
    public static float EnemyStatMultiplier => mapEnemyStatMultiplier * eventEnemyStatMultiplier;
    /// <summary>刷怪间隔综合倍率（地图 × 事件）。</summary>
    public static float SpawnIntervalMultiplier => mapSpawnIntervalMultiplier * eventSpawnIntervalMultiplier;
    /// <summary>单波最大刷怪数量倍率（仅地图）。</summary>
    public static float MaxSpawnCountMultiplier => mapMaxSpawnCountMultiplier;
    /// <summary>奖励倍率（事件叠加）。</summary>
    public static float RewardMultiplier => eventRewardMultiplier;
    /// <summary>是否因事件暂停刷怪。</summary>
    public static bool PauseSpawns => pauseSpawnsCount > 0;

    /// <summary>
    /// 设置当前地图并写入地图级波次修正。
    /// </summary>
    /// <param name="map">地图配置；为 null 时重置地图倍率为 1。</param>
    public static void SetMap(MapDataSO map)
    {
        currentMap = map;
        if (map == null)
        {
            mapEnemyStatMultiplier = 1f;
            mapSpawnIntervalMultiplier = 1f;
            mapMaxSpawnCountMultiplier = 1f;
            return;
        }

        MapWaveModifierConfig modifiers = map.WaveModifiers;
        mapEnemyStatMultiplier = modifiers.EnemyStatMultiplier;
        mapSpawnIntervalMultiplier = modifiers.SpawnIntervalMultiplier;
        mapMaxSpawnCountMultiplier = modifiers.MaxSpawnCountMultiplier;
    }

    /// <summary>重置所有事件级修正为默认值。</summary>
    public static void ResetEventModifiers()
    {
        eventEnemyStatMultiplier = 1f;
        eventSpawnIntervalMultiplier = 1f;
        eventRewardMultiplier = 1f;
        pauseSpawnsCount = 0;
    }

    /// <summary>
    /// 根据当前激活事件列表重新计算事件修正。
    /// </summary>
    /// <param name="activeEvents">正在生效的事件配置列表。</param>
    public static void RecomputeEventModifiers(IReadOnlyList<GameplayEventDataSO> activeEvents)
    {
        ResetEventModifiers();
        if (activeEvents == null || activeEvents.Count == 0)
        {
            return;
        }

        for (int i = 0; i < activeEvents.Count; i++)
        {
            ApplyEventEffects(activeEvents[i]);
        }
    }

    /// <summary>重置地图与事件相关的全部运行时修正。</summary>
    public static void Reset()
    {
        currentMap = null;
        mapEnemyStatMultiplier = 1f;
        mapSpawnIntervalMultiplier = 1f;
        mapMaxSpawnCountMultiplier = 1f;
        ResetEventModifiers();
    }

    /// <summary>将单条事件的所有效果叠加到事件修正。</summary>
    /// <param name="eventData">事件配置。</param>
    private static void ApplyEventEffects(GameplayEventDataSO eventData)
    {
        if (eventData == null)
        {
            return;
        }

        IReadOnlyList<GameplayEventEffectConfig> effects = eventData.Effects;
        if (effects == null)
        {
            return;
        }

        for (int i = 0; i < effects.Count; i++)
        {
            ApplyEffect(effects[i]);
        }
    }

    /// <summary>应用单条事件效果到运行时修正。</summary>
    /// <param name="effect">效果配置。</param>
    private static void ApplyEffect(GameplayEventEffectConfig effect)
    {
        float value = effect.Value <= 0f ? 1f : effect.Value;
        switch (effect.EffectType)
        {
            case GameplayEventEffectType.ModifySpawnInterval:
                eventSpawnIntervalMultiplier *= value;
                break;
            case GameplayEventEffectType.ModifyEnemyStats:
                eventEnemyStatMultiplier *= value;
                break;
            case GameplayEventEffectType.PauseSpawns:
                pauseSpawnsCount++;
                break;
            case GameplayEventEffectType.ModifyRewardMultiplier:
                eventRewardMultiplier *= value;
                break;
        }
    }
}
