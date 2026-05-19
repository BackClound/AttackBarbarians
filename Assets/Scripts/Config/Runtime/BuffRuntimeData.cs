using System.Collections.Generic;

/// <summary>
/// Buff 运行时实例：计时、层数与已应用的修正列表副本。
/// </summary>
public sealed class BuffRuntimeData
{
    public string ConfigId { get; private set; }
    public int Stacks { get; private set; }
    public float RemainingDuration { get; private set; }
    public bool IsPermanent { get; private set; }
    public IReadOnlyList<StatModifierConfig> Modifiers => modifiers;

    private readonly List<StatModifierConfig> modifiers = new List<StatModifierConfig>(4);

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

    public void Tick(float deltaTime)
    {
        if (IsPermanent)
        {
            return;
        }

        RemainingDuration -= deltaTime;
    }

    public bool IsExpired => !IsPermanent && RemainingDuration <= 0f;

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
