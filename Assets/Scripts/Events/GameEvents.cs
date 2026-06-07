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
    public static bool EnableDebugLogging { get; set; }

    #region Game

    public static void RaiseGameStateChanged(object sender, GameStateChange change) =>
        Publish(GameConstants.EventKeys.GameStateChanged, sender, change);

    public static void RaiseGameStarted(object sender = null) =>
        Publish(GameConstants.EventKeys.GameStarted, sender);

    public static void SubscribeGameStarted(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.GameStarted, handler);

    public static void UnsubscribeGameStarted(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.GameStarted, handler);

    public static void RaiseGamePaused(object sender = null) =>
        Publish(GameConstants.EventKeys.GamePaused, sender);

    public static void RaiseGameResumed(object sender = null) =>
        Publish(GameConstants.EventKeys.GameResumed, sender);

    public static void RaiseGameOver(object sender = null) =>
        Publish(GameConstants.EventKeys.GameOver, sender);

    public static void SubscribeGameOver(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.GameOver, handler);

    public static void UnsubscribeGameOver(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.GameOver, handler);

    public static void SubscribeGameStateChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.GameStateChanged, handler);

    public static void UnsubscribeGameStateChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.GameStateChanged, handler);

    /// <summary>与 <see cref="SubscribeGameStateChanged"/> 等价，便于对照模块文档中的 OnGameStateChanged 命名。</summary>
    public static void SubscribeOnGameStateChanged(Action<GameEventContext> handler) =>
        SubscribeGameStateChanged(handler);

    public static void UnsubscribeOnGameStateChanged(Action<GameEventContext> handler) =>
        UnsubscribeGameStateChanged(handler);

    #endregion

    #region Upgrade

    public static void RaiseUpgradeSelectionOpened(object sender = null) =>
        Publish(GameConstants.EventKeys.UpgradeSelectionOpened, sender);

    public static void RaiseUpgradeSelectionCompleted(object sender = null) =>
        Publish(GameConstants.EventKeys.UpgradeSelectionCompleted, sender);

    public static void SubscribeUpgradeSelectionOpened(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.UpgradeSelectionOpened, handler);

    public static void UnsubscribeUpgradeSelectionOpened(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.UpgradeSelectionOpened, handler);

    public static void SubscribeUpgradeSelectionCompleted(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.UpgradeSelectionCompleted, handler);

    public static void UnsubscribeUpgradeSelectionCompleted(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.UpgradeSelectionCompleted, handler);

    public static void RaiseUpgradeChoicesReady(object sender, UpgradeChoicesPayload payload) =>
        Publish(GameConstants.EventKeys.UpgradeChoicesReady, sender, payload);

    public static void SubscribeUpgradeChoicesReady(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.UpgradeChoicesReady, handler);

    public static void UnsubscribeUpgradeChoicesReady(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.UpgradeChoicesReady, handler);

    public static void RaiseUpgradeChoiceApplied(object sender, UpgradeChoiceAppliedPayload payload) =>
        Publish(GameConstants.EventKeys.UpgradeChoiceApplied, sender, payload);

    public static void SubscribeUpgradeChoiceApplied(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.UpgradeChoiceApplied, handler);

    public static void UnsubscribeUpgradeChoiceApplied(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.UpgradeChoiceApplied, handler);

    #endregion

    #region Wave

    public static void RaiseWaveStarted(object sender, WaveEventArgs args) =>
        Publish(GameConstants.EventKeys.WaveStarted, sender, args);

    public static void RaiseWaveCompleted(object sender, WaveEventArgs args) =>
        Publish(GameConstants.EventKeys.WaveCompleted, sender, args);

    public static void SubscribeWaveStarted(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.WaveStarted, handler);

    public static void UnsubscribeWaveStarted(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.WaveStarted, handler);

    public static void SubscribeWaveCompleted(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.WaveCompleted, handler);

    public static void UnsubscribeWaveCompleted(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.WaveCompleted, handler);

    #endregion

    #region Player

    public static void RaisePlayerDamaged(object sender, PlayerHealthEventArgs args) =>
        Publish(GameConstants.EventKeys.PlayerDamaged, sender, args);

    public static void RaisePlayerDied(object sender = null) =>
        Publish(GameConstants.EventKeys.PlayerDied, sender);

    public static void RaisePlayerHealthChanged(object sender, PlayerHealthEventArgs args) =>
        Publish(GameConstants.EventKeys.PlayerHealthChanged, sender, args);

    public static void RaisePlayerLevelUp(object sender, int newLevel) =>
        Publish(GameConstants.EventKeys.PlayerLevelUp, sender, newLevel);

    public static void SubscribePlayerLevelUp(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.PlayerLevelUp, handler);

    public static void UnsubscribePlayerLevelUp(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.PlayerLevelUp, handler);

    public static void RaisePlayerAttackStarted(object sender, PlayerAttackEventArgs args) =>
        Publish(GameConstants.EventKeys.PlayerAttackStarted, sender, args);

    public static void RaisePlayerSkillCast(object sender, string skillId) =>
        Publish(GameConstants.EventKeys.PlayerSkillCast, sender, skillId);

    public static void SubscribePlayerSkillCast(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.PlayerSkillCast, handler);

    public static void UnsubscribePlayerSkillCast(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.PlayerSkillCast, handler);

    public static void RaisePlayerStatsChanged(object sender, PlayerStatsChangedEventArgs args) =>
        Publish(GameConstants.EventKeys.PlayerStatsChanged, sender, args);

    public static void SubscribePlayerHealthChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.PlayerHealthChanged, handler);

    public static void UnsubscribePlayerHealthChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.PlayerHealthChanged, handler);

    public static void SubscribePlayerStatsChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.PlayerStatsChanged, handler);

    public static void UnsubscribePlayerStatsChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.PlayerStatsChanged, handler);

    public static void SubscribePlayerAttackStarted(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.PlayerAttackStarted, handler);

    public static void UnsubscribePlayerAttackStarted(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.PlayerAttackStarted, handler);

    public static void SubscribePlayerDamaged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.PlayerDamaged, handler);

    public static void UnsubscribePlayerDamaged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.PlayerDamaged, handler);

    public static void SubscribePlayerDied(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.PlayerDied, handler);

    public static void UnsubscribePlayerDied(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.PlayerDied, handler);

    #endregion

    #region Enemy

    public static void RaiseEnemySpawned(object sender, EnemyEventArgs args) =>
        Publish(GameConstants.EventKeys.EnemySpawned, sender, args);

    public static void RaiseEnemyKilled(object sender, EnemyEventArgs args) =>
        Publish(GameConstants.EventKeys.EnemyKilled, sender, args);

    public static void SubscribeEnemyKilled(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.EnemyKilled, handler);

    public static void UnsubscribeEnemyKilled(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.EnemyKilled, handler);

    #endregion

    #region Boss

    public static void RaiseBossSpawned(object sender, BossSpawnedEventArgs args) =>
        Publish(GameConstants.EventKeys.BossSpawned, sender, args);

    public static void RaiseBossPhaseChanged(object sender, BossPhaseChangedEventArgs args) =>
        Publish(GameConstants.EventKeys.BossPhaseChanged, sender, args);

    public static void RaiseBossDefeated(object sender, BossDefeatedEventArgs args) =>
        Publish(GameConstants.EventKeys.BossDefeated, sender, args);

    public static void SubscribeBossSpawned(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.BossSpawned, handler);

    public static void UnsubscribeBossSpawned(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.BossSpawned, handler);

    public static void SubscribeBossPhaseChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.BossPhaseChanged, handler);

    public static void UnsubscribeBossPhaseChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.BossPhaseChanged, handler);

    public static void SubscribeBossDefeated(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.BossDefeated, handler);

    public static void UnsubscribeBossDefeated(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.BossDefeated, handler);

    #endregion

    #region Elite

    public static void RaiseEliteSpawned(object sender, EliteSpawnedEventArgs args) =>
        Publish(GameConstants.EventKeys.EliteSpawned, sender, args);

    public static void SubscribeEliteSpawned(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.EliteSpawned, handler);

    public static void UnsubscribeEliteSpawned(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.EliteSpawned, handler);

    #endregion

    #region Special Enemy

    public static void RaiseSpecialEnemySpawned(object sender, SpecialEnemySpawnedEventArgs args) =>
        Publish(GameConstants.EventKeys.SpecialEnemySpawned, sender, args);

    public static void RaiseSpecialEnemyAbilityUsed(object sender, SpecialEnemyAbilityUsedEventArgs args) =>
        Publish(GameConstants.EventKeys.SpecialEnemyAbilityUsed, sender, args);

    public static void SubscribeSpecialEnemySpawned(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.SpecialEnemySpawned, handler);

    public static void UnsubscribeSpecialEnemySpawned(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.SpecialEnemySpawned, handler);

    public static void SubscribeSpecialEnemyAbilityUsed(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.SpecialEnemyAbilityUsed, handler);

    public static void UnsubscribeSpecialEnemyAbilityUsed(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.SpecialEnemyAbilityUsed, handler);

    #endregion

    #region Run

    public static void RaiseRunRewardSettled(object sender, RunRewardSettledEventArgs args) =>
        Publish(GameConstants.EventKeys.RunRewardSettled, sender, args);

    public static void SubscribeRunRewardSettled(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.RunRewardSettled, handler);

    public static void UnsubscribeRunRewardSettled(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.RunRewardSettled, handler);

    #endregion

    #region Damage

    public static void RaiseDamageApplied(object sender, DamageEventArgs args) =>
        Publish(GameConstants.EventKeys.DamageApplied, sender, args);

    public static void SubscribeDamageApplied(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.DamageApplied, handler);

    public static void UnsubscribeDamageApplied(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.DamageApplied, handler);

    #endregion

    #region Projectile

    public static void RaiseProjectileHit(object sender, ProjectileHitEventArgs args) =>
        Publish(GameConstants.EventKeys.ProjectileHit, sender, args);

    public static void SubscribeProjectileHit(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.ProjectileHit, handler);

    public static void UnsubscribeProjectileHit(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.ProjectileHit, handler);

    #endregion

    #region Skill

    public static void RaiseSkillUsed(object sender, string skillId) =>
        Publish(GameConstants.EventKeys.SkillUsed, sender, skillId);

    public static void RaiseSkillLevelUp(object sender, string skillId, int newLevel) =>
        Publish(GameConstants.EventKeys.SkillLevelUp, sender, new SkillLevelUpPayload(skillId, newLevel));

    public static void RaiseSkillUnlocked(object sender, string skillId) =>
        Publish(GameConstants.EventKeys.SkillUnlocked, sender, skillId);

    public static void SubscribeSkillUnlocked(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.SkillUnlocked, handler);

    public static void UnsubscribeSkillUnlocked(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.SkillUnlocked, handler);

    public static void SubscribeSkillUsed(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.SkillUsed, handler);

    public static void UnsubscribeSkillUsed(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.SkillUsed, handler);

    #endregion

    #region Buff

    public static void RaiseBuffApplied(object sender, BuffEventArgs args) =>
        Publish(GameConstants.EventKeys.BuffApplied, sender, args);

    public static void RaiseBuffRemoved(object sender, BuffEventArgs args) =>
        Publish(GameConstants.EventKeys.BuffRemoved, sender, args);

    public static void RaiseBuffChanged(object sender, BuffEventArgs args) =>
        Publish(GameConstants.EventKeys.BuffChanged, sender, args);

    public static void SubscribeBuffChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.BuffChanged, handler);

    public static void UnsubscribeBuffChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.BuffChanged, handler);

    #endregion

    #region UI

    public static void RaiseUiPanelOpened(object sender, string panelId) =>
        Publish(GameConstants.EventKeys.UiPanelOpened, sender, panelId);

    public static void RaiseUiPanelClosed(object sender, string panelId) =>
        Publish(GameConstants.EventKeys.UiPanelClosed, sender, panelId);

    public static void SubscribeUiPanelOpened(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.UiPanelOpened, handler);

    public static void UnsubscribeUiPanelOpened(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.UiPanelOpened, handler);

    public static void SubscribeUiPanelClosed(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.UiPanelClosed, handler);

    public static void UnsubscribeUiPanelClosed(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.UiPanelClosed, handler);

    #endregion

    #region Audio

    public static void RaiseAudioPlaySfx(object sender, string sfxId) =>
        Publish(GameConstants.EventKeys.AudioPlaySfx, sender, sfxId);

    public static void SubscribeAudioPlaySfx(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.AudioPlaySfx, handler);

    public static void UnsubscribeAudioPlaySfx(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.AudioPlaySfx, handler);

    public static void RaiseAudioPlayMusic(object sender, string musicId) =>
        Publish(GameConstants.EventKeys.AudioPlayMusic, sender, musicId);

    public static void SubscribeAudioPlayMusic(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.AudioPlayMusic, handler);

    public static void UnsubscribeAudioPlayMusic(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.AudioPlayMusic, handler);

    #endregion

    #region Save

    public static void RaiseSaveCompleted(object sender = null) =>
        Publish(GameConstants.EventKeys.SaveCompleted, sender);

    public static void RaiseSaveLoaded(object sender = null) =>
        Publish(GameConstants.EventKeys.SaveLoaded, sender);

    public static void SubscribeSaveCompleted(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.SaveCompleted, handler);

    public static void UnsubscribeSaveCompleted(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.SaveCompleted, handler);

    public static void SubscribeSaveLoaded(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.SaveLoaded, handler);

    public static void UnsubscribeSaveLoaded(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.SaveLoaded, handler);

    #endregion

    #region Talent

    public static void RaiseTalentChanged(object sender, TalentChangedEventArgs args) =>
        Publish(GameConstants.EventKeys.TalentChanged, sender, args);

    public static void SubscribeTalentChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.TalentChanged, handler);

    public static void UnsubscribeTalentChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.TalentChanged, handler);

    #endregion

    #region Equipment

    public static void RaiseEquipmentChanged(object sender, EquipmentChangedEventArgs args) =>
        Publish(GameConstants.EventKeys.EquipmentChanged, sender, args);

    public static void SubscribeEquipmentChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.EquipmentChanged, handler);

    public static void UnsubscribeEquipmentChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.EquipmentChanged, handler);

    #endregion

    #region Map

    public static void RaiseMapLoaded(object sender, MapLoadedEventArgs args) =>
        Publish(GameConstants.EventKeys.MapLoaded, sender, args);

    public static void SubscribeMapLoaded(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.MapLoaded, handler);

    public static void UnsubscribeMapLoaded(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.MapLoaded, handler);

    #endregion

    #region Economy

    public static void RaiseResourceChanged(object sender, ResourceChangedEventArgs args) =>
        Publish(GameConstants.EventKeys.ResourceChanged, sender, args);

    public static void SubscribeResourceChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.ResourceChanged, handler);

    public static void UnsubscribeResourceChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.ResourceChanged, handler);

    #endregion

    #region Shop

    public static void RaiseShopPurchased(object sender, ShopPurchaseEventArgs args) =>
        Publish(GameConstants.EventKeys.ShopPurchased, sender, args);

    public static void RaiseShopPurchaseFailed(
        object sender,
        string itemConfigId,
        ShopPurchaseFailedReason reason,
        string message) =>
        Publish(GameConstants.EventKeys.ShopPurchaseFailed, sender, new ShopPurchaseFailedEventArgs(itemConfigId, reason, message));

    public static void SubscribeShopPurchased(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.ShopPurchased, handler);

    public static void UnsubscribeShopPurchased(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.ShopPurchased, handler);

    public static void SubscribeShopPurchaseFailed(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.ShopPurchaseFailed, handler);

    public static void UnsubscribeShopPurchaseFailed(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.ShopPurchaseFailed, handler);

    #endregion

    #region Achievement

    public static void RaiseAchievementProgressChanged(object sender, AchievementProgressChangedEventArgs args) =>
        Publish(GameConstants.EventKeys.AchievementProgressChanged, sender, args);

    public static void RaiseAchievementClaimed(object sender, AchievementClaimedEventArgs args) =>
        Publish(GameConstants.EventKeys.AchievementClaimed, sender, args);

    public static void RaiseAchievementClaimFailed(object sender, AchievementClaimFailedEventArgs args) =>
        Publish(GameConstants.EventKeys.AchievementClaimFailed, sender, args);

    public static void SubscribeAchievementProgressChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.AchievementProgressChanged, handler);

    public static void UnsubscribeAchievementProgressChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.AchievementProgressChanged, handler);

    public static void SubscribeAchievementClaimed(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.AchievementClaimed, handler);

    public static void UnsubscribeAchievementClaimed(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.AchievementClaimed, handler);

    public static void SubscribeAchievementClaimFailed(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.AchievementClaimFailed, handler);

    public static void UnsubscribeAchievementClaimFailed(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.AchievementClaimFailed, handler);

    #endregion

    #region Daily Reward

    public static void RaiseDailyRewardClaimed(object sender, DailyRewardClaimedEventArgs args) =>
        Publish(GameConstants.EventKeys.DailyRewardClaimed, sender, args);

    public static void RaiseDailyRewardClaimFailed(object sender, DailyRewardClaimFailedEventArgs args) =>
        Publish(GameConstants.EventKeys.DailyRewardClaimFailed, sender, args);

    public static void RaiseDailyRewardStateChanged(object sender, DailyRewardStateChangedEventArgs args) =>
        Publish(GameConstants.EventKeys.DailyRewardStateChanged, sender, args);

    public static void SubscribeDailyRewardClaimed(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.DailyRewardClaimed, handler);

    public static void UnsubscribeDailyRewardClaimed(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.DailyRewardClaimed, handler);

    public static void SubscribeDailyRewardClaimFailed(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.DailyRewardClaimFailed, handler);

    public static void UnsubscribeDailyRewardClaimFailed(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.DailyRewardClaimFailed, handler);

    public static void SubscribeDailyRewardStateChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.DailyRewardStateChanged, handler);

    public static void UnsubscribeDailyRewardStateChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.DailyRewardStateChanged, handler);

    #endregion

    #region Ad

    public static void RaiseAdRewardCompleted(object sender, AdRewardCompletedEventArgs args) =>
        Publish(GameConstants.EventKeys.AdRewardCompleted, sender, args);

    public static void RaiseAdRewardFailed(object sender, AdRewardFailedEventArgs args) =>
        Publish(GameConstants.EventKeys.AdRewardFailed, sender, args);

    public static void RaiseAdRewardStateChanged(object sender, AdRewardStateChangedEventArgs args) =>
        Publish(GameConstants.EventKeys.AdRewardStateChanged, sender, args);

    public static void SubscribeAdRewardCompleted(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.AdRewardCompleted, handler);

    public static void UnsubscribeAdRewardCompleted(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.AdRewardCompleted, handler);

    public static void SubscribeAdRewardFailed(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.AdRewardFailed, handler);

    public static void UnsubscribeAdRewardFailed(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.AdRewardFailed, handler);

    public static void SubscribeAdRewardStateChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.AdRewardStateChanged, handler);

    public static void UnsubscribeAdRewardStateChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.AdRewardStateChanged, handler);

    #endregion

    #region Gameplay Event

    public static void RaiseGameplayEventStarted(object sender, GameplayEventArgs args) =>
        Publish(GameConstants.EventKeys.GameplayEventStarted, sender, args);

    public static void RaiseGameplayEventEnded(object sender, GameplayEventArgs args) =>
        Publish(GameConstants.EventKeys.GameplayEventEnded, sender, args);

    public static void SubscribeGameplayEventStarted(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.GameplayEventStarted, handler);

    public static void UnsubscribeGameplayEventStarted(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.GameplayEventStarted, handler);

    public static void SubscribeGameplayEventEnded(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.GameplayEventEnded, handler);

    public static void UnsubscribeGameplayEventEnded(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.GameplayEventEnded, handler);

    #endregion

    #region Bus access

    public static void Subscribe(string eventKey, Action<GameEventContext> handler)
    {
        if (!TryGetBus(out EventBus bus))
        {
            LogBusUnavailable("Subscribe", eventKey);
            return;
        }

        bus.Subscribe(eventKey, handler);
    }

    public static void Unsubscribe(string eventKey, Action<GameEventContext> handler)
    {
        if (!TryGetBus(out EventBus bus))
        {
            return;
        }

        bus.Unsubscribe(eventKey, handler);
    }

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

    private static bool TryGetBus(out EventBus bus) => ServiceLocator.TryGet(out bus);

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
    public string SkillId { get; }
    public int NewLevel { get; }

    public SkillLevelUpPayload(string skillId, int newLevel)
    {
        SkillId = skillId ?? string.Empty;
        NewLevel = newLevel;
    }
}
