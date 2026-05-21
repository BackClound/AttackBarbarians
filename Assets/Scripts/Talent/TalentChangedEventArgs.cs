/// <summary>
/// 天赋等级变更事件参数。
/// </summary>
public sealed class TalentChangedEventArgs
{
    public string TalentConfigId { get; }
    public int PreviousLevel { get; }
    public int NewLevel { get; }
    public long GoldSpent { get; }

    public TalentChangedEventArgs(string talentConfigId, int previousLevel, int newLevel, long goldSpent)
    {
        TalentConfigId = talentConfigId;
        PreviousLevel = previousLevel;
        NewLevel = newLevel;
        GoldSpent = goldSpent;
    }
}
