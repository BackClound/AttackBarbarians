using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 特殊敌人出现与能力提示（最小 HUD）：订阅 SpecialEnemy 事件。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>可选。挂在战斗 UI Canvas 上；未绑 UI 时仅 Debug 日志。</para>
/// </remarks>
public class SpecialEnemyHudPresenter : GameEventSubscriberBase
{
    [Header("UI (optional)")]
    [SerializeField] private Text toastText;

    [SerializeField] private float toastDuration = 2f;

    private float toastTimer;

    protected override void RegisterHandlers()
    {
        GameEvents.SubscribeSpecialEnemySpawned(OnSpecialSpawned);
        GameEvents.SubscribeSpecialEnemyAbilityUsed(OnAbilityUsed);
    }

    protected override void UnregisterHandlers()
    {
        GameEvents.UnsubscribeSpecialEnemySpawned(OnSpecialSpawned);
        GameEvents.UnsubscribeSpecialEnemyAbilityUsed(OnAbilityUsed);
    }

    private void Update()
    {
        if (toastText == null || toastTimer <= 0f)
        {
            return;
        }

        toastTimer -= Time.deltaTime;
        if (toastTimer <= 0f)
        {
            toastText.gameObject.SetActive(false);
        }
    }

    private void OnSpecialSpawned(GameEventContext ctx)
    {
        if (ctx.Payload is not SpecialEnemySpawnedEventArgs args)
        {
            return;
        }

        string message = $"特殊敌人: {args.EnemyConfigId} ({args.AbilityTags})";
        ShowToast(message);
        GameEvents.RaiseAudioPlaySfx(this, GameConstants.AudioIds.SfxSpecialEnemySpawn);
        Debug.Log($"[SpecialEnemyHud] {message}");
    }

    private void OnAbilityUsed(GameEventContext ctx)
    {
        if (ctx.Payload is not SpecialEnemyAbilityUsedEventArgs args)
        {
            return;
        }

        if (GameEvents.EnableDebugLogging)
        {
            Debug.Log($"[SpecialEnemyHud] 能力触发 {args.AbilityTag} config={args.AbilityConfigId}");
        }

        GameEvents.RaiseAudioPlaySfx(this, GameConstants.AudioIds.SfxSpecialEnemyAbility);
    }

    private void ShowToast(string message)
    {
        if (toastText == null)
        {
            return;
        }

        toastText.gameObject.SetActive(true);
        toastText.text = message;
        toastTimer = toastDuration;
    }
}
