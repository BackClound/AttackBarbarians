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

    private void OnEnable()
    {
        GameEvents.SubscribeEnemyKilled(OnEnemyKilled);
        GameEvents.SubscribeRunRewardSettled(OnRunRewardSettled);
        GameEvents.SubscribeGameStarted(OnGameStarted);
    }

    private void OnDisable()
    {
        GameEvents.UnsubscribeEnemyKilled(OnEnemyKilled);
        GameEvents.UnsubscribeRunRewardSettled(OnRunRewardSettled);
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
    }

    public void SetSessionKillCount(int kills)
    {
        sessionKills = Mathf.Max(0, kills);
    }

    protected override void OnShow()
    {
        RefreshDisplay();
    }

    private void OnGameStarted(GameEventContext ctx)
    {
        sessionKills = 0;
        lastSettlement = default;
    }

    private void OnEnemyKilled(GameEventContext ctx)
    {
        sessionKills++;
    }

    private void OnRunRewardSettled(GameEventContext ctx)
    {
        if (ctx.Payload is RunRewardSettledEventArgs args)
        {
            lastSettlement = args;
        }
    }

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

    private void OnRedeploy()
    {
        PlayUiSfx("audio.sfx.ui_confirm");
        if (ServiceLocator.TryGet(out SaveManager save))
        {
            save.BeginRun(1, 1);
        }

        if (ServiceLocator.TryGet(out GameManager gameManager))
        {
            gameManager.RestartGame();
        }
    }

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
