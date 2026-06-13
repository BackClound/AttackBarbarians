using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗 HUD：波次、计时、生命、经验、废料金、击杀、暂停。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。常显层，由 <see cref="UIManager"/> 在 Playing 状态启用。</para>
/// <para><b>布局：</b>中央 55% 留空给玩法区（见科技废土视觉规范）。</para>
/// </remarks>
public class GameplayHudPresenter : MonoBehaviour
{
    [Header("Bars")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text healthValueText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private Image expFill;
    [SerializeField] private TMP_Text levelText;

    [Header("Stats")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text killCountText;

    [Header("Actions")]
    [SerializeField] private Button pauseButton;

    [Header("Buff Row (optional)")]
    [SerializeField] private Transform buffIconRoot;
    [SerializeField] private GameObject buffIconPrefab;
    [SerializeField] private int maxBuffIcons = 8;

    private int sessionKillCount;
    private float hudRefreshTimer;
    private const float HudRefreshInterval = 0.15f;

    /// <summary>应用主题色并绑定暂停按钮。</summary>
    private void Awake()
    {
        ApplyThemeColors();

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(OnPauseClicked);
        }
    }

    /// <summary>订阅战斗 HUD 相关事件并全量刷新。</summary>
    private void OnEnable()
    {
        GameEvents.SubscribePlayerHealthChanged(OnPlayerHealthChanged);
        GameEvents.SubscribePlayerLevelUp(OnPlayerLevelUp);
        GameEvents.SubscribeWaveStarted(OnWaveStarted);
        GameEvents.SubscribeEnemyKilled(OnEnemyKilled);
        GameEvents.SubscribeBuffChanged(OnBuffChanged);
        GameEvents.SubscribeGameStarted(OnGameStarted);
        GameEvents.SubscribeRunRewardSettled(OnRunRewardSettled);
        GameEvents.SubscribeResourceChanged(OnResourceChanged);

        RefreshAll();
    }

    /// <summary>取消战斗 HUD 事件订阅。</summary>
    private void OnDisable()
    {
        GameEvents.UnsubscribePlayerHealthChanged(OnPlayerHealthChanged);
        GameEvents.UnsubscribePlayerLevelUp(OnPlayerLevelUp);
        GameEvents.UnsubscribeWaveStarted(OnWaveStarted);
        GameEvents.UnsubscribeEnemyKilled(OnEnemyKilled);
        GameEvents.UnsubscribeBuffChanged(OnBuffChanged);
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
        GameEvents.UnsubscribeRunRewardSettled(OnRunRewardSettled);
        GameEvents.UnsubscribeResourceChanged(OnResourceChanged);
    }

    /// <summary>按间隔合并刷新计时、金币与经验，避免每帧刷 TMP。</summary>
    private void Update()
    {
        hudRefreshTimer += Time.unscaledDeltaTime;
        if (hudRefreshTimer >= HudRefreshInterval)
        {
            hudRefreshTimer = 0f;
            RefreshTimer();
            RefreshGold();
            RefreshExperience();
        }
    }

    /// <summary>由 <see cref="UIManager"/> 控制 HUD 整体显隐。</summary>
    /// <param name="visible">是否显示 HUD。</param>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    /// <summary>本局累计击杀数，供结算面板读取。</summary>
    public int SessionKillCount => sessionKillCount;

    /// <summary>应用科技废土主题色到血条与经验条。</summary>
    private void ApplyThemeColors()
    {
        if (healthFill != null)
        {
            healthFill.color = UiTechWastelandPalette.HpFill;
        }

        if (expFill != null)
        {
            expFill.color = UiTechWastelandPalette.ExpFill;
        }
    }

    /// <summary>开局时重置击杀数并刷新 HUD。</summary>
    private void OnGameStarted(GameEventContext ctx)
    {
        sessionKillCount = 0;
        RefreshAll();
    }

    /// <summary>Run 结算后刷新金币显示。</summary>
    private void OnRunRewardSettled(GameEventContext ctx)
    {
        RefreshGold();
    }

    /// <summary>累计本局击杀数并刷新显示。</summary>
    private void OnEnemyKilled(GameEventContext ctx)
    {
        sessionKillCount++;
        RefreshKillCount();
    }

    /// <summary>波次开始时更新波次文本。</summary>
    private void OnWaveStarted(GameEventContext ctx)
    {
        if (ctx.Payload is WaveEventArgs args)
        {
            UpdateWaveDisplay(args.WaveIndex);
        }
    }

    /// <summary>根据事件更新生命条与数值颜色。</summary>
    private void OnPlayerHealthChanged(GameEventContext ctx)
    {
        if (ctx.Payload is not PlayerHealthEventArgs args)
        {
            return;
        }

        float maxHp = args.MaxHp > 0f ? args.MaxHp : 1f;
        float ratio = Mathf.Clamp01(args.CurrentHp / maxHp);

        if (healthSlider != null)
        {
            healthSlider.value = ratio;
        }

        if (healthValueText != null)
        {
            healthValueText.text = $"{Mathf.CeilToInt(args.CurrentHp)}/{Mathf.CeilToInt(maxHp)}";
            healthValueText.color = ratio < 0.25f
                ? UiTechWastelandPalette.DangerRed
                : UiTechWastelandPalette.TextPrimary;
        }
    }

    /// <summary>升级时刷新经验条与等级。</summary>
    private void OnPlayerLevelUp(GameEventContext ctx)
    {
        RefreshExperience();
    }

    /// <summary>Buff 变化回调（图标行占位）。</summary>
    private void OnBuffChanged(GameEventContext ctx)
    {
        // Buff 图标行：占位，后续可接 BuffManager 快照
    }

    /// <summary>暂停按钮回调，请求 GameManager 暂停。</summary>
    private void OnPauseClicked()
    {
        GameEvents.RaiseAudioPlaySfx(this, "audio.sfx.ui_click");
        if (ServiceLocator.TryGet(out GameManager gameManager) && gameManager.IsGameplayInputEnabled)
        {
            gameManager.PauseGame();
        }
    }

    /// <summary>刷新生命、经验、波次、计时、金币与击杀数。</summary>
    public void RefreshAll()
    {
        RefreshHealthFromPlayer();
        RefreshExperience();
        RefreshWave();
        RefreshTimer();
        RefreshGold();
        RefreshKillCount();
    }

    /// <summary>从场景玩家实体读取并刷新生命条。</summary>
    private void RefreshHealthFromPlayer()
    {
        if (!PlayerSceneAccess.TryGetHealth(out Player_Health health))
        {
            return;
        }

        float maxHp = health.MaxHp > 0f ? health.MaxHp : 1f;
        float ratio = Mathf.Clamp01(health.CurrentHp / maxHp);
        if (healthSlider != null)
        {
            healthSlider.value = ratio;
        }

        if (healthValueText != null)
        {
            healthValueText.text = $"{Mathf.CeilToInt(health.CurrentHp)}/{Mathf.CeilToInt(maxHp)}";
        }
    }

    /// <summary>从 PlayerRuntimeData 刷新经验与等级。</summary>
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
            levelText.text = $"LV {data.CurrentLevel}";
            levelText.color = UiTechWastelandPalette.TextTerminal;
        }
    }

    /// <summary>从 WaveManager 或存档读取当前波次。</summary>
    private void RefreshWave()
    {
        int wave = 1;
        if (ServiceLocator.TryGet(out WaveManager waveManager) && waveManager.IsInitialized)
        {
            wave = waveManager.CurrentWaveIndex;
        }
        else if (ServiceLocator.TryGet(out SaveManager save) && save.Current?.runProgress != null)
        {
            wave = Mathf.Max(1, save.Current.runProgress.currentWave);
        }

        UpdateWaveDisplay(wave);
    }

    /// <summary>更新波次 TMP 文案。</summary>
    private void UpdateWaveDisplay(int wave)
    {
        if (waveText != null)
        {
            waveText.text = $"WAVE-{wave}";
            waveText.color = UiTechWastelandPalette.TextTerminal;
        }
    }

    /// <summary>刷新本局存活计时显示。</summary>
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

    /// <summary>刷新废料金数量显示。</summary>
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

    /// <summary>资源事件触发时刷新金币。</summary>
    private void OnResourceChanged(GameEventContext ctx) => RefreshGold();

    /// <summary>刷新本局击杀数显示。</summary>
    private void RefreshKillCount()
    {
        if (killCountText != null)
        {
            killCountText.text = $"KILLS {sessionKillCount}";
            killCountText.color = UiTechWastelandPalette.TextSecondary;
        }
    }
}
