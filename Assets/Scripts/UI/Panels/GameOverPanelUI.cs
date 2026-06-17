using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 局末结算：时长、波次、击杀、奖励；再次部署 / 返回基地。
/// </summary>
public class GameOverPanelUI : UiPanelBase
{
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text durationText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text killText;
    [SerializeField] private TMP_Text goldRewardText;
    [SerializeField] private TMP_Text diamondRewardText;
    [SerializeField] private Button redeployButton;
    [SerializeField] private Button baseButton;
    [SerializeField] private string mainSceneName = "MainScene";

    private int sessionKills;
    private RunRewardSettledEventArgs lastSettlement;

    /// <summary>绑定再次部署与返回基地按钮。</summary>
    private void Awake()
    {
        if (redeployButton != null)
        {
            redeployButton.onClick.AddListener(OnRedeploy);
        }

        if (baseButton != null)
        {
            baseButton.onClick.AddListener(OnReturnBase);
        }
    }

    /// <summary>订阅击杀、结算与开局事件。</summary>
    private void OnEnable()
    {
        GameEvents.SubscribeEnemyKilled(OnEnemyKilled);
        GameEvents.SubscribeRunRewardSettled(OnRunRewardSettled);
        GameEvents.SubscribeGameStarted(OnGameStarted);
    }

    /// <summary>取消结算相关事件订阅。</summary>
    private void OnDisable()
    {
        GameEvents.UnsubscribeEnemyKilled(OnEnemyKilled);
        GameEvents.UnsubscribeRunRewardSettled(OnRunRewardSettled);
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
    }

    /// <summary>由 HUD 注入本局击杀数。</summary>
    /// <param name="kills">击杀总数。</param>
    public void SetSessionKillCount(int kills)
    {
        sessionKills = Mathf.Max(0, kills);
    }

    /// <summary>显示时刷新存活时长、波次、击杀与奖励数据。</summary>
    protected override void OnShow()
    {
        RefreshDisplay();
    }

    /// <summary>新局开始时重置击杀与结算缓存。</summary>
    private void OnGameStarted(GameEventContext ctx)
    {
        sessionKills = 0;
        lastSettlement = default;
    }

    /// <summary>累计本局击杀数。</summary>
    private void OnEnemyKilled(GameEventContext ctx)
    {
        sessionKills++;
    }

    /// <summary>缓存 Run 结算奖励数据。</summary>
    private void OnRunRewardSettled(GameEventContext ctx)
    {
        if (ctx.Payload is RunRewardSettledEventArgs args)
        {
            lastSettlement = args;
        }
    }

    /// <summary>刷新存活时长、波次、击杀与奖励文案。</summary>
    private void RefreshDisplay()
    {
        float duration = 0f;
        if (ServiceLocator.TryGet(out RunSessionTracker tracker))
        {
            duration = tracker.SessionDurationSeconds;
        }
        else if (lastSettlement.SessionDurationSeconds > 0f)
        {
            duration = lastSettlement.SessionDurationSeconds;
        }

        int wave = 1;
        if (ServiceLocator.TryGet(out WaveManager waveManager) && waveManager.IsInitialized)
        {
            wave = waveManager.CurrentWaveIndex;
        }
        else if (ServiceLocator.TryGet(out SaveManager save) && save.Current?.runProgress != null)
        {
            wave = Mathf.Max(1, save.Current.runProgress.currentWave);
        }

        if (headerText != null)
        {
            headerText.text = "任务终止 // REPORT";
            headerText.color = UiTechWastelandPalette.DangerRed;
        }

        if (durationText != null)
        {
            int minutes = Mathf.FloorToInt(duration / 60f);
            int seconds = Mathf.FloorToInt(duration % 60f);
            durationText.text = $"存活时长 {minutes:00}:{seconds:00}";
            durationText.color = UiTechWastelandPalette.TextPrimary;
        }

        if (waveText != null)
        {
            waveText.text = $"最高波次 WAVE-{wave}";
            waveText.color = UiTechWastelandPalette.TextPrimary;
        }

        if (killText != null)
        {
            killText.text = $"击杀数 {sessionKills}";
            killText.color = UiTechWastelandPalette.TextSecondary;
        }

        if (goldRewardText != null)
        {
            goldRewardText.text = $"+{lastSettlement.GoldGranted} 废料金";
            goldRewardText.color = UiTechWastelandPalette.AccentAmber;
        }

        if (diamondRewardText != null)
        {
            diamondRewardText.text = $"+{lastSettlement.DiamondsGranted} 量子钻";
            diamondRewardText.color = UiTechWastelandPalette.PrimaryCyan;
        }
    }

    /// <summary>再次部署按钮回调，重启战斗。</summary>
    private void OnRedeploy()
    {
        PlayUiSfx("audio.sfx.ui_confirm");
        if (ServiceLocator.TryGet(out GameManager gameManager))
        {
            gameManager.RestartGame();
        }
    }

    /// <summary>返回基地按钮回调。</summary>
    private void OnReturnBase()
    {
        PlayUiSfx("audio.sfx.ui_click");
        if (ServiceLocator.TryGet(out SaveManager save))
        {
            save.ClearActiveRun();
        }

        if (ServiceLocator.TryGet(out GameManager gameManager))
        {
            gameManager.OpenMainMenu();
        }

        if (!string.IsNullOrWhiteSpace(mainSceneName))
        {
            StartCoroutine(LoadMainSceneAfterBootstrapShutdown());
        }
    }

    /// <summary>销毁 Bootstrapper 后加载主场景。</summary>
    private IEnumerator LoadMainSceneAfterBootstrapShutdown()
    {
        if (GameBootstrapper.HasInstance)
        {
            Destroy(GameBootstrapper.Instance.gameObject);
            yield return null;
        }

        SceneManager.LoadScene(mainSceneName);
    }
}
