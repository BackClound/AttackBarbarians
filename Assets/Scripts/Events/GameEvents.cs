using System;
using UnityEngine;

/// <summary>
/// 全项目静态事件门面：按领域分区提供 Raise / Subscribe / Unsubscribe，内部委托 <see cref="EventBus"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。</para>
/// <para><b>发布：</b>业务代码只调用 <c>RaiseXxx</c>，不直接 <c>eventBus.Publish</c>（Manager 层可逐步迁移到本类）。</para>
/// <para><b>订阅：</b>MonoBehaviour 推荐继承 <see cref="GameEventSubscriberBase"/>；非 MonoBehaviour 在初始化时 Subscribe、Shutdown 时 Unsubscribe。</para>
/// <para><b>调试：</b>将 <see cref="EnableDebugLogging"/> 设为 true 可在 Console 看到事件 Key 与空总线警告。</para>
/// </remarks>
public static class GameEvents
{
    /// <summary>为 true 时在 Console 输出事件发布与总线不可用警告。</summary>
    public static bool EnableDebugLogging { get; set; }

    #region Game

    /// <summary>发布 Game State Changed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="change">状态变更负载。</param>
    public static void RaiseGameStateChanged(object sender, GameStateChange change) =>
        Publish(GameConstants.EventKeys.GameStateChanged, sender, change);

    /// <summary>发布 Game Started 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    public static void RaiseGameStarted(object sender = null) =>
        Publish(GameConstants.EventKeys.GameStarted, sender);

    /// <summary>订阅 Game Started 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeGameStarted(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.GameStarted, handler);

    /// <summary>取消订阅 Game Started 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeGameStarted(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.GameStarted, handler);

    /// <summary>发布 Game Paused 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    public static void RaiseGamePaused(object sender = null) =>
        Publish(GameConstants.EventKeys.GamePaused, sender);

    /// <summary>发布 Game Resumed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    public static void RaiseGameResumed(object sender = null) =>
        Publish(GameConstants.EventKeys.GameResumed, sender);

    /// <summary>发布 Game Over 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    public static void RaiseGameOver(object sender = null) =>
        Publish(GameConstants.EventKeys.GameOver, sender);

    /// <summary>订阅 Game Over 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeGameOver(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.GameOver, handler);

    /// <summary>取消订阅 Game Over 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeGameOver(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.GameOver, handler);

    /// <summary>订阅 Game State Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeGameStateChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.GameStateChanged, handler);

    /// <summary>取消订阅 Game State Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeGameStateChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.GameStateChanged, handler);

    /// <summary>订阅 On Game State Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeOnGameStateChanged(Action<GameEventContext> handler) =>
        SubscribeGameStateChanged(handler);

    /// <summary>取消订阅 On Game State Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeOnGameStateChanged(Action<GameEventContext> handler) =>
        UnsubscribeGameStateChanged(handler);

    #endregion

    #region Upgrade

    /// <summary>发布 Upgrade Selection Opened 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    public static void RaiseUpgradeSelectionOpened(object sender = null) =>
        Publish(GameConstants.EventKeys.UpgradeSelectionOpened, sender);

    /// <summary>发布 Upgrade Selection Completed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    public static void RaiseUpgradeSelectionCompleted(object sender = null) =>
        Publish(GameConstants.EventKeys.UpgradeSelectionCompleted, sender);

    /// <summary>订阅 Upgrade Selection Opened 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeUpgradeSelectionOpened(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.UpgradeSelectionOpened, handler);

    /// <summary>取消订阅 Upgrade Selection Opened 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeUpgradeSelectionOpened(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.UpgradeSelectionOpened, handler);

    /// <summary>订阅 Upgrade Selection Completed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeUpgradeSelectionCompleted(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.UpgradeSelectionCompleted, handler);

    /// <summary>取消订阅 Upgrade Selection Completed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeUpgradeSelectionCompleted(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.UpgradeSelectionCompleted, handler);

    /// <summary>发布 Upgrade Choices Ready 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="payload">事件负载。</param>
    public static void RaiseUpgradeChoicesReady(object sender, UpgradeChoicesPayload payload) =>
        Publish(GameConstants.EventKeys.UpgradeChoicesReady, sender, payload);

    /// <summary>订阅 Upgrade Choices Ready 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeUpgradeChoicesReady(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.UpgradeChoicesReady, handler);

    /// <summary>取消订阅 Upgrade Choices Ready 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeUpgradeChoicesReady(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.UpgradeChoicesReady, handler);

    /// <summary>发布 Upgrade Choice Applied 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="payload">事件负载。</param>
    public static void RaiseUpgradeChoiceApplied(object sender, UpgradeChoiceAppliedPayload payload) =>
        Publish(GameConstants.EventKeys.UpgradeChoiceApplied, sender, payload);

    /// <summary>订阅 Upgrade Choice Applied 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeUpgradeChoiceApplied(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.UpgradeChoiceApplied, handler);

    /// <summary>取消订阅 Upgrade Choice Applied 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeUpgradeChoiceApplied(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.UpgradeChoiceApplied, handler);

    #endregion

    #region Wave

    /// <summary>发布 Wave Started 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseWaveStarted(object sender, WaveEventArgs args) =>
        Publish(GameConstants.EventKeys.WaveStarted, sender, args);

    /// <summary>发布 Wave Completed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseWaveCompleted(object sender, WaveEventArgs args) =>
        Publish(GameConstants.EventKeys.WaveCompleted, sender, args);

    /// <summary>订阅 Wave Started 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeWaveStarted(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.WaveStarted, handler);

    /// <summary>取消订阅 Wave Started 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeWaveStarted(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.WaveStarted, handler);

    /// <summary>订阅 Wave Completed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeWaveCompleted(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.WaveCompleted, handler);

    /// <summary>取消订阅 Wave Completed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeWaveCompleted(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.WaveCompleted, handler);

    #endregion

    #region Player

    /// <summary>发布 Player Damaged 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaisePlayerDamaged(object sender, PlayerHealthEventArgs args) =>
        Publish(GameConstants.EventKeys.PlayerDamaged, sender, args);

    /// <summary>发布 Player Died 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    public static void RaisePlayerDied(object sender = null) =>
        Publish(GameConstants.EventKeys.PlayerDied, sender);

    /// <summary>发布 Player Health Changed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaisePlayerHealthChanged(object sender, PlayerHealthEventArgs args) =>
        Publish(GameConstants.EventKeys.PlayerHealthChanged, sender, args);

    /// <summary>发布 Player Level Up 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="newLevel">升级后的等级。</param>
    public static void RaisePlayerLevelUp(object sender, int newLevel) =>
        Publish(GameConstants.EventKeys.PlayerLevelUp, sender, newLevel);

    /// <summary>订阅 Player Level Up 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribePlayerLevelUp(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.PlayerLevelUp, handler);

    /// <summary>取消订阅 Player Level Up 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribePlayerLevelUp(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.PlayerLevelUp, handler);

    /// <summary>发布 Player Attack Started 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaisePlayerAttackStarted(object sender, PlayerAttackEventArgs args) =>
        Publish(GameConstants.EventKeys.PlayerAttackStarted, sender, args);

    /// <summary>发布 Player Skill Cast 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="skillId">技能配置 ID。</param>
    public static void RaisePlayerSkillCast(object sender, string skillId) =>
        Publish(GameConstants.EventKeys.PlayerSkillCast, sender, skillId);

    /// <summary>订阅 Player Skill Cast 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribePlayerSkillCast(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.PlayerSkillCast, handler);

    /// <summary>取消订阅 Player Skill Cast 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribePlayerSkillCast(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.PlayerSkillCast, handler);

    /// <summary>发布 Player Stats Changed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaisePlayerStatsChanged(object sender, PlayerStatsChangedEventArgs args) =>
        Publish(GameConstants.EventKeys.PlayerStatsChanged, sender, args);

    /// <summary>订阅 Player Health Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribePlayerHealthChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.PlayerHealthChanged, handler);

    /// <summary>取消订阅 Player Health Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribePlayerHealthChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.PlayerHealthChanged, handler);

    /// <summary>订阅 Player Stats Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribePlayerStatsChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.PlayerStatsChanged, handler);

    /// <summary>取消订阅 Player Stats Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribePlayerStatsChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.PlayerStatsChanged, handler);

    /// <summary>订阅 Player Attack Started 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribePlayerAttackStarted(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.PlayerAttackStarted, handler);

    /// <summary>取消订阅 Player Attack Started 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribePlayerAttackStarted(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.PlayerAttackStarted, handler);

    /// <summary>订阅 Player Damaged 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribePlayerDamaged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.PlayerDamaged, handler);

    /// <summary>取消订阅 Player Damaged 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribePlayerDamaged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.PlayerDamaged, handler);

    /// <summary>订阅 Player Died 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribePlayerDied(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.PlayerDied, handler);

    /// <summary>取消订阅 Player Died 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribePlayerDied(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.PlayerDied, handler);

    #endregion

    #region Enemy

    /// <summary>发布 Enemy Spawned 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseEnemySpawned(object sender, EnemyEventArgs args) =>
        Publish(GameConstants.EventKeys.EnemySpawned, sender, args);

    /// <summary>发布 Enemy Killed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseEnemyKilled(object sender, EnemyEventArgs args) =>
        Publish(GameConstants.EventKeys.EnemyKilled, sender, args);

    /// <summary>订阅 Enemy Killed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeEnemyKilled(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.EnemyKilled, handler);

    /// <summary>取消订阅 Enemy Killed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeEnemyKilled(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.EnemyKilled, handler);

    #endregion

    #region Boss

    /// <summary>发布 Boss Spawned 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseBossSpawned(object sender, BossSpawnedEventArgs args) =>
        Publish(GameConstants.EventKeys.BossSpawned, sender, args);

    /// <summary>发布 Boss Phase Changed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseBossPhaseChanged(object sender, BossPhaseChangedEventArgs args) =>
        Publish(GameConstants.EventKeys.BossPhaseChanged, sender, args);

    /// <summary>发布 Boss Defeated 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseBossDefeated(object sender, BossDefeatedEventArgs args) =>
        Publish(GameConstants.EventKeys.BossDefeated, sender, args);

    /// <summary>订阅 Boss Spawned 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeBossSpawned(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.BossSpawned, handler);

    /// <summary>取消订阅 Boss Spawned 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeBossSpawned(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.BossSpawned, handler);

    /// <summary>订阅 Boss Phase Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeBossPhaseChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.BossPhaseChanged, handler);

    /// <summary>取消订阅 Boss Phase Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeBossPhaseChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.BossPhaseChanged, handler);

    /// <summary>订阅 Boss Defeated 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeBossDefeated(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.BossDefeated, handler);

    /// <summary>取消订阅 Boss Defeated 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeBossDefeated(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.BossDefeated, handler);

    #endregion

    #region Elite

    /// <summary>发布 Elite Spawned 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseEliteSpawned(object sender, EliteSpawnedEventArgs args) =>
        Publish(GameConstants.EventKeys.EliteSpawned, sender, args);

    /// <summary>订阅 Elite Spawned 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeEliteSpawned(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.EliteSpawned, handler);

    /// <summary>取消订阅 Elite Spawned 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeEliteSpawned(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.EliteSpawned, handler);

    #endregion

    #region Special Enemy

    /// <summary>发布 Special Enemy Spawned 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseSpecialEnemySpawned(object sender, SpecialEnemySpawnedEventArgs args) =>
        Publish(GameConstants.EventKeys.SpecialEnemySpawned, sender, args);

    /// <summary>发布 Special Enemy Ability Used 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseSpecialEnemyAbilityUsed(object sender, SpecialEnemyAbilityUsedEventArgs args) =>
        Publish(GameConstants.EventKeys.SpecialEnemyAbilityUsed, sender, args);

    /// <summary>订阅 Special Enemy Spawned 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeSpecialEnemySpawned(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.SpecialEnemySpawned, handler);

    /// <summary>取消订阅 Special Enemy Spawned 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeSpecialEnemySpawned(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.SpecialEnemySpawned, handler);

    /// <summary>订阅 Special Enemy Ability Used 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeSpecialEnemyAbilityUsed(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.SpecialEnemyAbilityUsed, handler);

    /// <summary>取消订阅 Special Enemy Ability Used 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeSpecialEnemyAbilityUsed(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.SpecialEnemyAbilityUsed, handler);

    #endregion

    #region Run

    /// <summary>发布 Run Reward Settled 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseRunRewardSettled(object sender, RunRewardSettledEventArgs args) =>
        Publish(GameConstants.EventKeys.RunRewardSettled, sender, args);

    /// <summary>订阅 Run Reward Settled 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeRunRewardSettled(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.RunRewardSettled, handler);

    /// <summary>取消订阅 Run Reward Settled 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeRunRewardSettled(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.RunRewardSettled, handler);

    #endregion

    #region Damage

    /// <summary>发布 Damage Applied 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseDamageApplied(object sender, DamageEventArgs args) =>
        Publish(GameConstants.EventKeys.DamageApplied, sender, args);

    /// <summary>订阅 Damage Applied 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeDamageApplied(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.DamageApplied, handler);

    /// <summary>取消订阅 Damage Applied 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeDamageApplied(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.DamageApplied, handler);

    #endregion

    #region Projectile

    /// <summary>发布 Projectile Hit 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseProjectileHit(object sender, ProjectileHitEventArgs args) =>
        Publish(GameConstants.EventKeys.ProjectileHit, sender, args);

    /// <summary>订阅 Projectile Hit 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeProjectileHit(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.ProjectileHit, handler);

    /// <summary>取消订阅 Projectile Hit 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeProjectileHit(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.ProjectileHit, handler);

    #endregion

    #region Skill

    /// <summary>发布 Skill Used 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="skillId">技能配置 ID。</param>
    public static void RaiseSkillUsed(object sender, string skillId) =>
        Publish(GameConstants.EventKeys.SkillUsed, sender, skillId);

    /// <summary>发布 Skill Level Up 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="skillId">技能配置 ID。</param>
    /// <param name="newLevel">升级后的等级。</param>
    public static void RaiseSkillLevelUp(object sender, string skillId, int newLevel) =>
        Publish(GameConstants.EventKeys.SkillLevelUp, sender, new SkillLevelUpPayload(skillId, newLevel));

    /// <summary>发布 Skill Unlocked 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="skillId">技能配置 ID。</param>
    public static void RaiseSkillUnlocked(object sender, string skillId) =>
        Publish(GameConstants.EventKeys.SkillUnlocked, sender, skillId);

    /// <summary>订阅 Skill Unlocked 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeSkillUnlocked(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.SkillUnlocked, handler);

    /// <summary>取消订阅 Skill Unlocked 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeSkillUnlocked(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.SkillUnlocked, handler);

    /// <summary>订阅 Skill Used 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeSkillUsed(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.SkillUsed, handler);

    /// <summary>取消订阅 Skill Used 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeSkillUsed(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.SkillUsed, handler);

    #endregion

    #region Buff

    /// <summary>发布 Buff Applied 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseBuffApplied(object sender, BuffEventArgs args) =>
        Publish(GameConstants.EventKeys.BuffApplied, sender, args);

    /// <summary>发布 Buff Removed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseBuffRemoved(object sender, BuffEventArgs args) =>
        Publish(GameConstants.EventKeys.BuffRemoved, sender, args);

    /// <summary>发布 Buff Changed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseBuffChanged(object sender, BuffEventArgs args) =>
        Publish(GameConstants.EventKeys.BuffChanged, sender, args);

    /// <summary>订阅 Buff Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeBuffChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.BuffChanged, handler);

    /// <summary>取消订阅 Buff Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeBuffChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.BuffChanged, handler);

    #endregion

    #region UI

    /// <summary>发布 Ui Panel Opened 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="panelId">UI 面板 ID。</param>
    public static void RaiseUiPanelOpened(object sender, string panelId) =>
        Publish(GameConstants.EventKeys.UiPanelOpened, sender, panelId);

    /// <summary>发布 Ui Panel Closed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="panelId">UI 面板 ID。</param>
    public static void RaiseUiPanelClosed(object sender, string panelId) =>
        Publish(GameConstants.EventKeys.UiPanelClosed, sender, panelId);

    /// <summary>订阅 Ui Panel Opened 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeUiPanelOpened(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.UiPanelOpened, handler);

    /// <summary>取消订阅 Ui Panel Opened 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeUiPanelOpened(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.UiPanelOpened, handler);

    /// <summary>订阅 Ui Panel Closed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeUiPanelClosed(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.UiPanelClosed, handler);

    /// <summary>取消订阅 Ui Panel Closed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeUiPanelClosed(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.UiPanelClosed, handler);

    #endregion

    #region Audio

    /// <summary>发布 Audio Play Sfx 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="sfxId">音效 ID。</param>
    public static void RaiseAudioPlaySfx(object sender, string sfxId) =>
        Publish(GameConstants.EventKeys.AudioPlaySfx, sender, sfxId);

    /// <summary>订阅 Audio Play Sfx 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeAudioPlaySfx(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.AudioPlaySfx, handler);

    /// <summary>取消订阅 Audio Play Sfx 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeAudioPlaySfx(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.AudioPlaySfx, handler);

    /// <summary>发布 Audio Play Music 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="musicId">音乐 ID。</param>
    public static void RaiseAudioPlayMusic(object sender, string musicId) =>
        Publish(GameConstants.EventKeys.AudioPlayMusic, sender, musicId);

    /// <summary>订阅 Audio Play Music 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeAudioPlayMusic(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.AudioPlayMusic, handler);

    /// <summary>取消订阅 Audio Play Music 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeAudioPlayMusic(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.AudioPlayMusic, handler);

    #endregion

    #region Save

    /// <summary>发布 Save Completed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    public static void RaiseSaveCompleted(object sender = null) =>
        Publish(GameConstants.EventKeys.SaveCompleted, sender);

    /// <summary>发布 Save Loaded 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    public static void RaiseSaveLoaded(object sender = null) =>
        Publish(GameConstants.EventKeys.SaveLoaded, sender);

    /// <summary>订阅 Save Completed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeSaveCompleted(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.SaveCompleted, handler);

    /// <summary>取消订阅 Save Completed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeSaveCompleted(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.SaveCompleted, handler);

    /// <summary>订阅 Save Loaded 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeSaveLoaded(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.SaveLoaded, handler);

    /// <summary>取消订阅 Save Loaded 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeSaveLoaded(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.SaveLoaded, handler);

    #endregion

    #region Talent

    /// <summary>发布 Talent Changed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseTalentChanged(object sender, TalentChangedEventArgs args) =>
        Publish(GameConstants.EventKeys.TalentChanged, sender, args);

    /// <summary>订阅 Talent Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeTalentChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.TalentChanged, handler);

    /// <summary>取消订阅 Talent Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeTalentChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.TalentChanged, handler);

    #endregion

    #region Equipment

    /// <summary>发布 Equipment Changed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseEquipmentChanged(object sender, EquipmentChangedEventArgs args) =>
        Publish(GameConstants.EventKeys.EquipmentChanged, sender, args);

    /// <summary>订阅 Equipment Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeEquipmentChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.EquipmentChanged, handler);

    /// <summary>取消订阅 Equipment Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeEquipmentChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.EquipmentChanged, handler);

    #endregion

    #region Map

    /// <summary>发布 Map Loaded 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseMapLoaded(object sender, MapLoadedEventArgs args) =>
        Publish(GameConstants.EventKeys.MapLoaded, sender, args);

    /// <summary>订阅 Map Loaded 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeMapLoaded(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.MapLoaded, handler);

    /// <summary>取消订阅 Map Loaded 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeMapLoaded(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.MapLoaded, handler);

    #endregion

    #region Economy

    /// <summary>发布 Resource Changed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseResourceChanged(object sender, ResourceChangedEventArgs args) =>
        Publish(GameConstants.EventKeys.ResourceChanged, sender, args);

    /// <summary>订阅 Resource Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeResourceChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.ResourceChanged, handler);

    /// <summary>取消订阅 Resource Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeResourceChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.ResourceChanged, handler);

    #endregion

    #region Shop

    /// <summary>发布 Shop Purchased 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseShopPurchased(object sender, ShopPurchaseEventArgs args) =>
        Publish(GameConstants.EventKeys.ShopPurchased, sender, args);

    /// <summary>发布 Shop Purchase Failed 事件。</summary>
    public static void RaiseShopPurchaseFailed(
        object sender,
        string itemConfigId,
        ShopPurchaseFailedReason reason,
        string message) =>
        Publish(GameConstants.EventKeys.ShopPurchaseFailed, sender, new ShopPurchaseFailedEventArgs(itemConfigId, reason, message));

    /// <summary>订阅 Shop Purchased 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeShopPurchased(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.ShopPurchased, handler);

    /// <summary>取消订阅 Shop Purchased 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeShopPurchased(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.ShopPurchased, handler);

    /// <summary>订阅 Shop Purchase Failed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeShopPurchaseFailed(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.ShopPurchaseFailed, handler);

    /// <summary>取消订阅 Shop Purchase Failed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeShopPurchaseFailed(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.ShopPurchaseFailed, handler);

    #endregion

    #region Achievement

    /// <summary>发布 Achievement Progress Changed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseAchievementProgressChanged(object sender, AchievementProgressChangedEventArgs args) =>
        Publish(GameConstants.EventKeys.AchievementProgressChanged, sender, args);

    /// <summary>发布 Achievement Claimed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseAchievementClaimed(object sender, AchievementClaimedEventArgs args) =>
        Publish(GameConstants.EventKeys.AchievementClaimed, sender, args);

    /// <summary>发布 Achievement Claim Failed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseAchievementClaimFailed(object sender, AchievementClaimFailedEventArgs args) =>
        Publish(GameConstants.EventKeys.AchievementClaimFailed, sender, args);

    /// <summary>订阅 Achievement Progress Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeAchievementProgressChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.AchievementProgressChanged, handler);

    /// <summary>取消订阅 Achievement Progress Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeAchievementProgressChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.AchievementProgressChanged, handler);

    /// <summary>订阅 Achievement Claimed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeAchievementClaimed(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.AchievementClaimed, handler);

    /// <summary>取消订阅 Achievement Claimed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeAchievementClaimed(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.AchievementClaimed, handler);

    /// <summary>订阅 Achievement Claim Failed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeAchievementClaimFailed(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.AchievementClaimFailed, handler);

    /// <summary>取消订阅 Achievement Claim Failed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeAchievementClaimFailed(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.AchievementClaimFailed, handler);

    #endregion

    #region Daily Reward

    /// <summary>发布 Daily Reward Claimed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseDailyRewardClaimed(object sender, DailyRewardClaimedEventArgs args) =>
        Publish(GameConstants.EventKeys.DailyRewardClaimed, sender, args);

    /// <summary>发布 Daily Reward Claim Failed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseDailyRewardClaimFailed(object sender, DailyRewardClaimFailedEventArgs args) =>
        Publish(GameConstants.EventKeys.DailyRewardClaimFailed, sender, args);

    /// <summary>发布 Daily Reward State Changed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseDailyRewardStateChanged(object sender, DailyRewardStateChangedEventArgs args) =>
        Publish(GameConstants.EventKeys.DailyRewardStateChanged, sender, args);

    /// <summary>订阅 Daily Reward Claimed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeDailyRewardClaimed(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.DailyRewardClaimed, handler);

    /// <summary>取消订阅 Daily Reward Claimed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeDailyRewardClaimed(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.DailyRewardClaimed, handler);

    /// <summary>订阅 Daily Reward Claim Failed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeDailyRewardClaimFailed(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.DailyRewardClaimFailed, handler);

    /// <summary>取消订阅 Daily Reward Claim Failed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeDailyRewardClaimFailed(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.DailyRewardClaimFailed, handler);

    /// <summary>订阅 Daily Reward State Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeDailyRewardStateChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.DailyRewardStateChanged, handler);

    /// <summary>取消订阅 Daily Reward State Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeDailyRewardStateChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.DailyRewardStateChanged, handler);

    #endregion

    #region Upgrade Card

    /// <summary>发布 Upgrade Card Granted 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseUpgradeCardGranted(object sender, UpgradeCardGrantedEventArgs args) =>
        Publish(GameConstants.EventKeys.UpgradeCardGranted, sender, args);

    /// <summary>订阅 Upgrade Card Granted 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeUpgradeCardGranted(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.UpgradeCardGranted, handler);

    /// <summary>取消订阅 Upgrade Card Granted 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeUpgradeCardGranted(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.UpgradeCardGranted, handler);

    #endregion

    #region Ad

    /// <summary>发布 Ad Reward Completed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseAdRewardCompleted(object sender, AdRewardCompletedEventArgs args) =>
        Publish(GameConstants.EventKeys.AdRewardCompleted, sender, args);

    /// <summary>发布 Ad Reward Failed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseAdRewardFailed(object sender, AdRewardFailedEventArgs args) =>
        Publish(GameConstants.EventKeys.AdRewardFailed, sender, args);

    /// <summary>发布 Ad Reward State Changed 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseAdRewardStateChanged(object sender, AdRewardStateChangedEventArgs args) =>
        Publish(GameConstants.EventKeys.AdRewardStateChanged, sender, args);

    /// <summary>订阅 Ad Reward Completed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeAdRewardCompleted(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.AdRewardCompleted, handler);

    /// <summary>取消订阅 Ad Reward Completed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeAdRewardCompleted(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.AdRewardCompleted, handler);

    /// <summary>订阅 Ad Reward Failed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeAdRewardFailed(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.AdRewardFailed, handler);

    /// <summary>取消订阅 Ad Reward Failed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeAdRewardFailed(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.AdRewardFailed, handler);

    /// <summary>订阅 Ad Reward State Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeAdRewardStateChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.AdRewardStateChanged, handler);

    /// <summary>取消订阅 Ad Reward State Changed 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeAdRewardStateChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.AdRewardStateChanged, handler);

    #endregion

    #region Gameplay Event

    /// <summary>发布 Gameplay Event Started 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseGameplayEventStarted(object sender, GameplayEventArgs args) =>
        Publish(GameConstants.EventKeys.GameplayEventStarted, sender, args);

    /// <summary>发布 Gameplay Event Ended 事件。</summary>
    /// <param name="sender">事件发送者，可为 null。</param>
    /// <param name="args">事件负载。</param>
    public static void RaiseGameplayEventEnded(object sender, GameplayEventArgs args) =>
        Publish(GameConstants.EventKeys.GameplayEventEnded, sender, args);

    /// <summary>订阅 Gameplay Event Started 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeGameplayEventStarted(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.GameplayEventStarted, handler);

    /// <summary>取消订阅 Gameplay Event Started 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeGameplayEventStarted(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.GameplayEventStarted, handler);

    /// <summary>订阅 Gameplay Event Ended 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void SubscribeGameplayEventEnded(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.GameplayEventEnded, handler);

    /// <summary>取消订阅 Gameplay Event Ended 事件。</summary>
    /// <param name="handler">事件回调。</param>
    public static void UnsubscribeGameplayEventEnded(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.GameplayEventEnded, handler);

    #endregion

    #region Bus access

    /// <summary>底层 订阅 指定 Key 的事件。</summary>
    /// <param name="eventKey">事件 Key。</param>
    /// <param name="handler">回调委托。</param>
    public static void Subscribe(string eventKey, Action<GameEventContext> handler)
    {
        if (!TryGetBus(out EventBus bus))
        {
            LogBusUnavailable("Subscribe", eventKey);
            return;
        }

        bus.Subscribe(eventKey, handler);
    }

    /// <summary>底层 取消订阅 指定 Key 的事件。</summary>
    /// <param name="eventKey">事件 Key。</param>
    /// <param name="handler">回调委托。</param>
    public static void Unsubscribe(string eventKey, Action<GameEventContext> handler)
    {
        if (!TryGetBus(out EventBus bus))
        {
            return;
        }

        bus.Unsubscribe(eventKey, handler);
    }

    /// <summary>向 <see cref="EventBus"/> 发布事件；总线未就绪时记录警告并跳过。</summary>
    /// <param name="eventKey">事件 Key。</param>
    /// <param name="sender">发送者，可为 null。</param>
    /// <param name="payload">负载，可为 null。</param>
    private static void Publish(string eventKey, object sender = null, object payload = null)
    {
        if (!TryGetBus(out EventBus bus))
        {
            LogBusUnavailable("Publish", eventKey);
            return;
        }

        if (EnableDebugLogging)
        {
            string payloadName = payload != null ? payload.GetType().Name : "null";
            Debug.Log($"[GameEvents] Publish {eventKey} sender={sender} payload={payloadName}");
        }

        bus.Publish(eventKey, sender, payload);
    }

    /// <summary>尝试从 <see cref="ServiceLocator"/> 获取 <see cref="EventBus"/>。</summary>
    /// <param name="bus">输出事件总线。</param>
    /// <returns>总线已注册时返回 true。</returns>
    private static bool TryGetBus(out EventBus bus) => ServiceLocator.TryGet(out bus);

    /// <summary>在调试模式下记录 EventBus 未就绪警告。</summary>
    /// <param name="operation">操作名称（Subscribe/Publish）。</param>
    /// <param name="eventKey">事件 Key。</param>
    private static void LogBusUnavailable(string operation, string eventKey)
    {
        if (!EnableDebugLogging)
        {
            return;
        }

        Debug.LogWarning($"[GameEvents] {operation} skipped: EventBus not ready. key={eventKey}");
    }

    #endregion
}

/// <summary>
/// 技能升级事件负载。
/// </summary>
public readonly struct SkillLevelUpPayload
{
    /// <summary>技能配置 ID。</summary>
    public string SkillId { get; }
    /// <summary>升级后的等级。</summary>
    public int NewLevel { get; }

    /// <summary>构造技能升级负载。</summary>
    /// <param name="skillId">技能 ID。</param>
    /// <param name="newLevel">新等级。</param>
    public SkillLevelUpPayload(string skillId, int newLevel)
    {
        SkillId = skillId ?? string.Empty;
        NewLevel = newLevel;
    }
}
