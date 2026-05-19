using System;

/// <summary>
/// 累计统计：局数、最高波次、击杀、游玩时长等。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。嵌套在 <see cref="SaveData"/> 中。</para>
/// </remarks>
[Serializable]
public class SaveStatisticsData
{
    public int totalRuns;
    public int highestWave;
    public long totalKills;
    public long totalPlayTimeSeconds;

    public static SaveStatisticsData CreateDefault() => new SaveStatisticsData();
}
