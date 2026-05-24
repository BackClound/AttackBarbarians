/// <summary>
/// 局内随机事件效果类型。
/// </summary>
public enum GameplayEventEffectType
{
    None = 0,
    // 修改生成间隔
    ModifySpawnInterval = 1,
    // 修改敌人属性
    ModifyEnemyStats = 2,
    // 暂停生成 
    PauseSpawns = 3,
    // 应用玩家Buff
    ApplyPlayerBuff = 4,
    // 修改奖励倍率
    ModifyRewardMultiplier = 5
}
