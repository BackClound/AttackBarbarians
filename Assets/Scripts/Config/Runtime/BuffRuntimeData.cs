using System.Collections.Generic;

/// <summary>
/// Buff 运行时实例：计时、层数与已应用的修正列表副本。
/// </summary>
public sealed class BuffRuntimeData
{
    /// <summary>Buff 配置唯一标识。</summary>
    public string ConfigId { get; private set; }

    /// <summary>当前层数。</summary>
    public int Stacks { get; private set; }

    /// <summary>剩余持续时间（秒）；永久 Buff 为无穷大。</summary>
    public float RemainingDuration { get; private set; }

    /// <summary>是否为永久 Buff。</summary>
    public bool IsPermanent { get; private set; }

    /// <summary>已复制的属性修正列表。</summary>
    public IReadOnlyList<StatModifierConfig> Modifiers => modifiers;

    private readonly List<StatModifierConfig> modifiers = new List<StatModifierConfig>(4);

    /// <summary>
    /// 从 Buff 配置资产初始化运行时实例，复制修正列表并设置初始层数与持续时间。
    /// </summary>
    /// <param name="source">来源 Buff 配置；为 null 时不执行任何操作。</param>
    /// <param name="stacks">初始层数，默认为 1。</param>
    public void Initialize(BuffDataSO source, int stacks = 1)
    {
        if (source == null)
        {
            return;
        }

        ConfigId = source.ConfigId;
        IsPermanent = source.IsPermanent;
        RemainingDuration = source.IsPermanent ? float.PositiveInfinity : source.Duration;
        Stacks = UnityEngine.Mathf.Clamp(stacks, 1, source.MaxStacks);

        modifiers.Clear();
        if (source.Modifiers != null)
        {
            for (int i = 0; i < source.Modifiers.Count; i++)
            {
                StatModifierConfig mod = source.Modifiers[i];
                if (mod != null)
                {
                    modifiers.Add(mod);
                }
            }
        }
    }

    /// <summary>
    /// 推进 Buff 剩余持续时间，永久 Buff 不受影响。
    /// </summary>
    /// <param name="deltaTime">经过的时间（秒）。</param>
    public void Tick(float deltaTime)
    {
        if (IsPermanent)
        {
            return;
        }

        RemainingDuration -= deltaTime;
    }

    /// <summary>Buff 是否已过期（永久 Buff 始终为 false）。</summary>
    public bool IsExpired => !IsPermanent && RemainingDuration <= 0f;

    /// <summary>
    /// 增加 Buff 层数，并在配置允许时刷新持续时间。
    /// </summary>
    /// <param name="source">来源 Buff 配置，用于读取最大层数与刷新规则；为 null 时不执行任何操作。</param>
    public void AddStack(BuffDataSO source)
    {
        if (source == null)
        {
            return;
        }

        if (Stacks < source.MaxStacks)
        {
            Stacks++;
        }

        if (source.RefreshDurationOnStack && !source.IsPermanent)
        {
            RemainingDuration = source.Duration;
        }
    }
}
