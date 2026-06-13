using UnityEngine;

/// <summary>
/// 玩家运行时数据：仅保存 configId、等级与属性快照，不持有 SO 引用（便于存档）。
/// </summary>
public sealed class PlayerRuntimeData
{
    /// <summary>玩家配置唯一标识（对应 <see cref="PlayerDataSO.ConfigId"/>）。</summary>
    public string ConfigId { get; private set; }

    /// <summary>当前等级。</summary>
    public int Level { get; private set; }

    /// <summary>当前累计经验值。</summary>
    public float CurrentExperience { get; private set; }

    /// <summary>运行时属性快照，可叠加 Buff 修正。</summary>
    public StatRuntimeSnapshot Stats { get; } = new StatRuntimeSnapshot();

    /// <summary>
    /// 从玩家配置资产初始化运行时数据。
    /// </summary>
    /// <param name="source">来源玩家配置；为 null 时不执行任何操作。</param>
    /// <param name="level">初始等级；-1 时使用配置中的起始等级。</param>
    public void Initialize(PlayerDataSO source, int level = -1)
    {
        if (source == null)
        {
            return;
        }

        ConfigId = source.ConfigId;
        Level = level > 0 ? level : source.StartLevel;
        CurrentExperience = 0f;
        Stats.CopyFromBlock(source.BaseStats);
    }

    /// <summary>
    /// 设置玩家当前等级，最小为 1。
    /// </summary>
    /// <param name="level">目标等级。</param>
    public void SetLevel(int level)
    {
        Level = level < 1 ? 1 : level;
    }

    /// <summary>
    /// 增加当前经验值，结果不会低于 0。
    /// </summary>
    /// <param name="amount">要增加的经验数量。</param>
    public void AddExperience(float amount)
    {
        CurrentExperience = Mathf.Max(0f, CurrentExperience + amount);
    }

    /// <summary>当前经验值（只读别名）。</summary>
    public float CurrentExperienceValue => CurrentExperience;

    /// <summary>当前等级（只读别名）。</summary>
    public int CurrentLevel => Level;
}
