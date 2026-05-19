/// <summary>
/// 玩家运行时数据：仅保存 configId、等级与属性快照，不持有 SO 引用（便于存档）。
/// </summary>
public sealed class PlayerRuntimeData
{
    public string ConfigId { get; private set; }
    public int Level { get; private set; }
    public float CurrentExperience { get; private set; }
    public StatRuntimeSnapshot Stats { get; } = new StatRuntimeSnapshot();

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

    public void SetLevel(int level)
    {
        Level = level < 1 ? 1 : level;
    }

    public void AddExperience(float amount)
    {
        CurrentExperience += amount;
    }
}
