/// <summary>
/// 局内随机事件效果类型。
/// </summary>
public enum GameplayEventEffectType
{
    /// <summary>无效果。</summary>
    None = 0,
    /// <summary>修改刷怪间隔倍率。</summary>
    ModifySpawnInterval = 1,
    /// <summary>修改敌人属性倍率。</summary>
    ModifyEnemyStats = 2,
    /// <summary>暂停刷怪。</summary>
    PauseSpawns = 3,
    /// <summary>为玩家施加 Buff。</summary>
    ApplyPlayerBuff = 4,
    /// <summary>修改局末奖励倍率。</summary>
    ModifyRewardMultiplier = 5
}
