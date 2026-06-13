/// <summary>
/// 成就目标类型：由 <see cref="AchievementManager"/> 监听对应事件并累计进度。
/// </summary>
public enum AchievementTargetType
{
    /// <summary>累计击杀敌人数量。</summary>
    TotalEnemyKills = 0,
    /// <summary>累计击败 Boss 数量。</summary>
    TotalBossDefeats = 1,
    /// <summary>达到的最高波次。</summary>
    HighestWaveReached = 2,
    /// <summary>累计完成波次数量。</summary>
    TotalWavesCompleted = 3,
    /// <summary>累计游玩次数。</summary>
    TotalRunsPlayed = 4,
    /// <summary>累计获得金币数量。</summary>
    LifetimeGoldEarned = 5,
    /// <summary>玩家等级达到指定值。</summary>
    PlayerLevelReached = 6,
}
