/// <summary>
/// 天赋等级变更事件参数。
/// </summary>
public sealed class TalentChangedEventArgs
{
    /// <summary>天赋配置 ID。</summary>
    public string TalentConfigId { get; }
    /// <summary>升级前等级。</summary>
    public int PreviousLevel { get; }
    /// <summary>升级后等级。</summary>
    public int NewLevel { get; }
    /// <summary>本次升级消耗的金币。</summary>
    public long GoldSpent { get; }

    /// <summary>
    /// 创建天赋等级变更事件参数。
    /// </summary>
    /// <param name="talentConfigId">天赋配置 ID。</param>
    /// <param name="previousLevel">升级前等级。</param>
    /// <param name="newLevel">升级后等级。</param>
    /// <param name="goldSpent">本次升级消耗的金币。</param>
    public TalentChangedEventArgs(string talentConfigId, int previousLevel, int newLevel, long goldSpent)
    {
        TalentConfigId = talentConfigId;
        PreviousLevel = previousLevel;
        NewLevel = newLevel;
        GoldSpent = goldSpent;
    }
}
