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

    public static void RaiseGamePaused(object sender = null) =>
        Publish(GameConstants.EventKeys.GamePaused, sender);

    public static void RaiseGameResumed(object sender = null) =>
        Publish(GameConstants.EventKeys.GameResumed, sender);

    public static void RaiseGameOver(object sender = null) =>
        Publish(GameConstants.EventKeys.GameOver, sender);

    public static void SubscribeGameStateChanged(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.GameStateChanged, handler);

    public static void UnsubscribeGameStateChanged(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.GameStateChanged, handler);

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

    #region Damage

    public static void RaiseDamageApplied(object sender, DamageEventArgs args) =>
        Publish(GameConstants.EventKeys.DamageApplied, sender, args);

    public static void SubscribeDamageApplied(Action<GameEventContext> handler) =>
        Subscribe(GameConstants.EventKeys.DamageApplied, handler);

    public static void UnsubscribeDamageApplied(Action<GameEventContext> handler) =>
        Unsubscribe(GameConstants.EventKeys.DamageApplied, handler);

    #endregion

    #region Skill

    public static void RaiseSkillUsed(object sender, string skillId) =>
        Publish(GameConstants.EventKeys.SkillUsed, sender, skillId);

    public static void RaiseSkillLevelUp(object sender, string skillId, int newLevel) =>
        Publish(GameConstants.EventKeys.SkillLevelUp, sender, new SkillLevelUpPayload(skillId, newLevel));

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

    #endregion

    #region Audio

    public static void RaiseAudioPlaySfx(object sender, string sfxId) =>
        Publish(GameConstants.EventKeys.AudioPlaySfx, sender, sfxId);

    public static void RaiseAudioPlayMusic(object sender, string musicId) =>
        Publish(GameConstants.EventKeys.AudioPlayMusic, sender, musicId);

    #endregion

    #region Save

    public static void RaiseSaveCompleted(object sender = null) =>
        Publish(GameConstants.EventKeys.SaveCompleted, sender);

    public static void RaiseSaveLoaded(object sender = null) =>
        Publish(GameConstants.EventKeys.SaveLoaded, sender);

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
