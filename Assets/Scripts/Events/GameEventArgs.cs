using UnityEngine;

/// <summary>
/// 伤害事件负载，通过 <see cref="GameConstants.EventKeys.DamageApplied"/> 发布。
/// </summary>
/// <remarks>纯数据结构，无需挂载。</remarks>
public readonly struct DamageEventArgs
{
    public float Amount { get; }
    public Vector3 WorldPosition { get; }
    public object Source { get; }
    public GameObject Target { get; }
    public bool IsCritical { get; }

    public DamageEventArgs(
        float amount,
        Vector3 worldPosition,
        object source,
        GameObject target,
        bool isCritical = false)
    {
        Amount = amount;
        WorldPosition = worldPosition;
        Source = source;
        Target = target;
        IsCritical = isCritical;
    }
}

/// <summary>
/// 敌人相关事件负载（击杀、生成等）。
/// </summary>
/// <remarks>纯数据结构，无需挂载。订阅方优先使用 <see cref="EnemyObject"/> 与 <see cref="Position"/>，避免依赖具体 Enemy 子类。</remarks>
public readonly struct EnemyEventArgs
{
    public GameObject EnemyObject { get; }
    public Vector3 Position { get; }
    public object Killer { get; }
    public string EnemyTypeId { get; }

    public EnemyEventArgs(GameObject enemyObject, Vector3 position, object killer, string enemyTypeId = null)
    {
        EnemyObject = enemyObject;
        Position = position;
        Killer = killer;
        EnemyTypeId = enemyTypeId ?? string.Empty;
    }
}

/// <summary>
/// 波次事件负载。
/// </summary>
/// <remarks>纯数据结构，无需挂载。</remarks>
public readonly struct WaveEventArgs
{
    public int WaveIndex { get; }
    public float DurationSeconds { get; }
    public int EnemyCount { get; }

    public WaveEventArgs(int waveIndex, float durationSeconds = 0f, int enemyCount = 0)
    {
        WaveIndex = waveIndex;
        DurationSeconds = durationSeconds;
        EnemyCount = enemyCount;
    }
}

/// <summary>
/// Buff 变更事件负载。
/// </summary>
/// <remarks>纯数据结构，无需挂载。</remarks>
public readonly struct BuffEventArgs
{
    public string BuffId { get; }
    public int Stacks { get; }
    public float RemainingSeconds { get; }
    public object Target { get; }

    public BuffEventArgs(string buffId, int stacks, float remainingSeconds, object target)
    {
        BuffId = buffId ?? string.Empty;
        Stacks = stacks;
        RemainingSeconds = remainingSeconds;
        Target = target;
    }
}

/// <summary>
/// 玩家受伤/血量变更事件负载。
/// </summary>
/// <remarks>纯数据结构，无需挂载。</remarks>
public readonly struct PlayerHealthEventArgs
{
    public float CurrentHp { get; }
    public float MaxHp { get; }
    public float Delta { get; }
    public object Source { get; }

    public PlayerHealthEventArgs(float currentHp, float maxHp, float delta, object source)
    {
        CurrentHp = currentHp;
        MaxHp = maxHp;
        Delta = delta;
        Source = source;
    }
}
