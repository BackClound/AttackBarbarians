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

    public static MapDataSO CurrentMap => currentMap;
    public static float EnemyStatMultiplier => mapEnemyStatMultiplier * eventEnemyStatMultiplier;
    public static float SpawnIntervalMultiplier => mapSpawnIntervalMultiplier * eventSpawnIntervalMultiplier;
    public static float MaxSpawnCountMultiplier => mapMaxSpawnCountMultiplier;
    public static float RewardMultiplier => eventRewardMultiplier;
    public static bool PauseSpawns => pauseSpawnsCount > 0;

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

    public static void ResetEventModifiers()
    {
        eventEnemyStatMultiplier = 1f;
        eventSpawnIntervalMultiplier = 1f;
        eventRewardMultiplier = 1f;
        pauseSpawnsCount = 0;
    }

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

    public static void Reset()
    {
        currentMap = null;
        mapEnemyStatMultiplier = 1f;
        mapSpawnIntervalMultiplier = 1f;
        mapMaxSpawnCountMultiplier = 1f;
        ResetEventModifiers();
    }

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
