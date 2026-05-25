/// <summary>
/// 成就目标类型：由 <see cref="AchievementManager"/> 监听对应事件并累计进度。
/// </summary>
public enum AchievementTargetType
{
    // 总击杀敌人数量
    TotalEnemyKills = 0,
    // 总击败Boss数量
    TotalBossDefeats = 1,
    // 最高波次
    HighestWaveReached = 2,
    // 总完成波次
    TotalWavesCompleted = 3,
    // 总游玩次数
    TotalRunsPlayed = 4,
    // 总获得金币数量
    LifetimeGoldEarned = 5,
    // 玩家等级达到
    PlayerLevelReached = 6,
}
