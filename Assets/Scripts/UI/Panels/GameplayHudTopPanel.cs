using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗 HUD 顶栏：暂停、计时、关卡名、等级/经验、波次。
/// </summary>
public class GameplayHudTopPanel : MonoBehaviour
{
    [Header("Stage")]
    [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private Image expFill;

    [Header("Run Info")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private Button pauseButton;

    [Header("Optional")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text killCountText;

    private int sessionKillCount;
    private float refreshTimer;
    private const float RefreshInterval = 0.15f;

    /// <summary>本局击杀数（供 Presenter 读取）。</summary>
    public int SessionKillCount => sessionKillCount;

    /// <summary>应用主题色并绑定暂停按钮。</summary>
    private void Awake()
    {
        if (expFill != null)
        {
            expFill.color = UiTechWastelandPalette.ExpFill;
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(OnPauseClicked);
        }
    }

    /// <summary>订阅顶栏相关事件。</summary>
    private void OnEnable()
    {
        GameEvents.SubscribePlayerLevelUp(OnPlayerLevelUp);
        GameEvents.SubscribeWaveStarted(OnWaveStarted);
        GameEvents.SubscribeEnemyKilled(OnEnemyKilled);
        GameEvents.SubscribeGameStarted(OnGameStarted);
        GameEvents.SubscribeRunRewardSettled(OnRunRewardSettled);
        GameEvents.SubscribeResourceChanged(OnResourceChanged);
        RefreshAll();
    }

    /// <summary>取消事件订阅。</summary>
    private void OnDisable()
    {
        GameEvents.UnsubscribePlayerLevelUp(OnPlayerLevelUp);
        GameEvents.UnsubscribeWaveStarted(OnWaveStarted);
        GameEvents.UnsubscribeEnemyKilled(OnEnemyKilled);
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
        GameEvents.UnsubscribeRunRewardSettled(OnRunRewardSettled);
        GameEvents.UnsubscribeResourceChanged(OnResourceChanged);
    }

    /// <summary>合并刷新计时、金币与经验。</summary>
    private void Update()
    {
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer >= RefreshInterval)
        {
            refreshTimer = 0f;
            RefreshTimer();
            RefreshGold();
            RefreshExperience();
        }
    }

    /// <summary>全量刷新顶栏。</summary>
    public void RefreshAll()
    {
        RefreshStageName();
        RefreshExperience();
        RefreshWave();
        RefreshTimer();
        RefreshGold();
        RefreshKillCount();
    }

    /// <summary>重置击杀数。</summary>
    public void ResetSessionKillCount()
    {
        sessionKillCount = 0;
        RefreshKillCount();
    }

    private void OnGameStarted(GameEventContext ctx)
    {
        sessionKillCount = 0;
        RefreshAll();
    }

    private void OnRunRewardSettled(GameEventContext ctx) => RefreshGold();

    private void OnEnemyKilled(GameEventContext ctx)
    {
        sessionKillCount++;
        RefreshKillCount();
    }

    private void OnWaveStarted(GameEventContext ctx)
    {
        if (ctx.Payload is WaveEventArgs args)
        {
            UpdateWaveDisplay(args.WaveIndex);
        }
    }

    private void OnPlayerLevelUp(GameEventContext ctx) => RefreshExperience();

    private void OnResourceChanged(GameEventContext ctx) => RefreshGold();

    private void OnPauseClicked()
    {
        GameEvents.RaiseAudioPlaySfx(this, "audio.sfx.ui_click");
        if (ServiceLocator.TryGet(out GameManager gameManager) && gameManager.IsGameplayInputEnabled)
        {
            gameManager.PauseGame();
        }
    }

    private void RefreshStageName()
    {
        if (stageNameText == null)
        {
            return;
        }

        string stageName = "废墟前线";
        if (MapRuntimeContext.CurrentMap != null)
        {
            stageName = MapRuntimeContext.CurrentMap.DisplayName;
        }

        stageNameText.text = stageName;
        stageNameText.color = UiTechWastelandPalette.TextPrimary;
    }

    private void RefreshExperience()
    {
        if (!PlayerSceneAccess.TryGetController(out PlayerController controller) ||
            !controller.IsReady ||
            controller.ActiveData == null)
        {
            return;
        }

        PlayerRuntimeData data = controller.RuntimeStats.Data;
        float perLevel = Mathf.Max(1f, controller.ActiveData.ExperiencePerLevel);
        float ratio = Mathf.Clamp01(data.CurrentExperienceValue / perLevel);

        if (expSlider != null)
        {
            expSlider.value = ratio;
        }

        if (levelText != null)
        {
            levelText.text = $"{data.CurrentLevel}级";
            levelText.color = UiTechWastelandPalette.TextPrimary;
        }
    }

    private void RefreshWave()
    {
        int wave = 1;
        int total = 20;
        if (ServiceLocator.TryGet(out WaveManager waveManager) && waveManager.IsInitialized)
        {
            wave = waveManager.CurrentWaveIndex;
            total = waveManager.TotalWaveCount;
        }
        else if (ServiceLocator.TryGet(out SaveManager save) && save.Current?.runProgress != null)
        {
            wave = Mathf.Max(1, save.Current.runProgress.currentWave);
        }

        UpdateWaveDisplay(wave, total);
    }

    private void UpdateWaveDisplay(int wave, int total = 20)
    {
        if (waveText == null)
        {
            return;
        }

        waveText.text = $"波次: {wave}/{total}";
        waveText.color = UiTechWastelandPalette.TextPrimary;
    }

    private void RefreshTimer()
    {
        if (timerText == null)
        {
            return;
        }

        float seconds = 0f;
        if (ServiceLocator.TryGet(out RunSessionTracker tracker))
        {
            seconds = tracker.SessionDurationSeconds;
        }
        else if (ServiceLocator.TryGet(out WaveManager waveManager))
        {
            seconds = waveManager.WaveElapsed;
        }

        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        timerText.text = $"{minutes:00}:{secs:00}";
        timerText.color = UiTechWastelandPalette.TextPrimary;
    }

    private void RefreshGold()
    {
        if (goldText == null)
        {
            return;
        }

        long gold = 0;
        if (ServiceLocator.TryGet(out ResourceManager resources))
        {
            gold = resources.GetAmount(CurrencyType.Gold);
        }
        else if (ServiceLocator.TryGet(out SaveManager save) && save.Current != null)
        {
            gold = save.Current.gold;
        }

        goldText.text = gold.ToString();
        goldText.color = UiTechWastelandPalette.AccentAmber;
    }

    private void RefreshKillCount()
    {
        if (killCountText == null)
        {
            return;
        }

        killCountText.text = $"击杀 {sessionKillCount}";
        killCountText.color = UiTechWastelandPalette.TextSecondary;
    }
}
