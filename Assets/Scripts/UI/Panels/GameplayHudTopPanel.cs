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
        GameEvents.SubscribeOnGameStateChanged(OnGameStateChanged);
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
        GameEvents.UnsubscribeOnGameStateChanged(OnGameStateChanged);
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

    /// <summary>游戏开始时重置击杀数并全量刷新顶栏。</summary>
    /// <param name="ctx">游戏开始事件上下文。</param>
    private void OnGameStarted(GameEventContext ctx)
    {
        sessionKillCount = 0;
        RefreshAll();
    }

    /// <summary>局内奖励结算后刷新金币显示。</summary>
    /// <param name="ctx">结算事件上下文。</param>
    private void OnRunRewardSettled(GameEventContext ctx) => RefreshGold();

    /// <summary>敌人击杀时累加本局击杀数并刷新显示。</summary>
    /// <param name="ctx">击杀事件上下文。</param>
    private void OnEnemyKilled(GameEventContext ctx)
    {
        sessionKillCount++;
        RefreshKillCount();
    }

    /// <summary>新波次开始时更新波次文本并刷新经验。</summary>
    /// <param name="ctx">波次开始事件上下文。</param>
    private void OnWaveStarted(GameEventContext ctx)
    {
        if (ctx.Payload is WaveEventArgs args)
        {
            UpdateWaveDisplay(args.WaveIndex);
        }

        RefreshExperience();
    }

    /// <summary>玩家升级后刷新经验条与等级文本。</summary>
    /// <param name="ctx">升级事件上下文。</param>
    private void OnPlayerLevelUp(GameEventContext ctx) => RefreshExperience();

    /// <summary>游戏状态变更后刷新经验（暂停等可能影响显示）。</summary>
    /// <param name="ctx">状态变更事件上下文。</param>
    private void OnGameStateChanged(GameEventContext ctx) => RefreshExperience();

    /// <summary>资源变更后刷新金币显示。</summary>
    /// <param name="ctx">资源变更事件上下文。</param>
    private void OnResourceChanged(GameEventContext ctx) => RefreshGold();

    /// <summary>暂停按钮点击：播放音效并请求暂停游戏。</summary>
    private void OnPauseClicked()
    {
        GameEvents.RaiseAudioPlaySfx(this, "audio.sfx.ui_click");
        if (ServiceLocator.TryGet(out GameManager gameManager) && gameManager.IsGameplayInputEnabled)
        {
            gameManager.PauseGame();
        }
    }

    /// <summary>刷新关卡名称文本（优先当前地图显示名）。</summary>
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

    /// <summary>从玩家运行时数据刷新经验条与等级。</summary>
    private void RefreshExperience()
    {
        if (!PlayerSceneAccess.TryGetController(out PlayerController controller) ||
            !controller.IsReady ||
            controller.ActiveData == null)
        {
            return;
        }

        PlayerRuntimeData data = controller.RuntimeStats.Data;
        float perLevel = Mathf.Max(1f, controller.GetNeedExperienceForCurrentLevel());
        // 升级三选一弹窗期间显示满格（本级已完成），确认后经验归零再从 0 累计。
        float ratio = controller.IsLevelUpPending
            ? 1f
            : Mathf.Clamp01(data.CurrentExperienceValue / perLevel);

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

    /// <summary>从 WaveManager 或存档解析当前波次并更新显示。</summary>
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

    /// <summary>写入波次文本（当前波 / 总波数）。</summary>
    /// <param name="wave">当前波次索引。</param>
    /// <param name="total">总波次数。</param>
    private void UpdateWaveDisplay(int wave, int total = 20)
    {
        if (waveText == null)
        {
            return;
        }

        waveText.text = total > 0 ? $"波次: {wave}/{total}" : $"波次: {wave}";
        waveText.color = UiTechWastelandPalette.TextPrimary;
    }

    /// <summary>刷新本局计时器（优先 RunSessionTracker）。</summary>
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

    /// <summary>刷新金币文本（优先 ResourceManager）。</summary>
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

    /// <summary>刷新本局击杀数文本。</summary>
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
