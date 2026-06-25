using UnityEngine;

/// <summary>
/// 局内升级选择调度器：监听波次/升级事件触发三选一 UI 流程（单局成长，非 Meta 持久化）。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。</para>
/// <para><b>推荐挂载对象：</b><c>GameSystems</c> 子物体，与 <see cref="UpgradeManager"/> 同层级。</para>
/// <para><b>测试：</b>升级阶段 Inspector 右键 <c>Debug/Select Choice 0</c>，或开启 <c>Auto Confirm First Choice</c>。</para>
/// </remarks>
public class RandomRewardManager : MonoBehaviour, IGameSystem
{
    [Header("Debug")]
    [SerializeField] private bool autoConfirmFirstChoiceForDebug;

    [Header("Ad Quota (per run)")]
    [SerializeField] private int maxAdRerollsPerRun = 2;
    [SerializeField] private int maxAdSelectAllPerRun = 2;

    private UpgradeManager upgradeManager;
    private GameFlowManager gameFlowManager;
    private UpgradeChoicesPayload pendingPayload;
    private bool isInitialized;
    private int usedAdRerolls;
    private int usedAdSelectAll;

    /// <summary>是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;
    /// <summary>当前待选的升级候选负载。</summary>
    public UpgradeChoicesPayload PendingChoices => pendingPayload;
    /// <summary>本局剩余广告刷新次数。</summary>
    public int RemainingAdRerolls => Mathf.Max(0, maxAdRerollsPerRun - usedAdRerolls);
    /// <summary>本局剩余广告全选次数。</summary>
    public int RemainingAdSelectAll => Mathf.Max(0, maxAdSelectAllPerRun - usedAdSelectAll);
    /// <summary>本局广告刷新上限。</summary>
    public int MaxAdRerollsPerRun => maxAdRerollsPerRun;
    /// <summary>本局广告全选上限。</summary>
    public int MaxAdSelectAllPerRun => maxAdSelectAllPerRun;

    /// <summary>订阅升级事件并初始化依赖。</summary>
    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        upgradeManager = ResolveUpgradeManager();
        ServiceLocator.TryGet(out gameFlowManager);

        GameEvents.SubscribeUpgradeSelectionOpened(OnUpgradeSelectionOpened);
        GameEvents.SubscribePlayerLevelUp(OnPlayerLevelUp);
        GameEvents.SubscribeGameStarted(OnGameStarted);
        isInitialized = true;
    }

    /// <summary>每帧更新（随机奖励系统无逐帧逻辑）。</summary>
    /// <param name="deltaTime">距上一帧的秒数。</param>
    public void Tick(float deltaTime) { }

    /// <summary>取消事件订阅并清空待选状态。</summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeUpgradeSelectionOpened(OnUpgradeSelectionOpened);
        GameEvents.UnsubscribePlayerLevelUp(OnPlayerLevelUp);
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
        pendingPayload = null;
        isInitialized = false;
    }

    /// <summary>UI 或调试入口：选择第 index 个候选并结束升级阶段。</summary>
    /// <param name="index">候选索引（0 起）。</param>
    /// <returns>选择并应用成功返回 <c>true</c>。</returns>
    public bool TrySelectChoice(int index)
    {
        if (pendingPayload == null || pendingPayload.Choices == null || pendingPayload.Choices.Count == 0)
        {
            Debug.LogWarning("[RandomRewardManager] 当前没有待选升级。");
            return false;
        }

        if (index < 0 || index >= pendingPayload.Choices.Count)
        {
            Debug.LogWarning($"[RandomRewardManager] 无效选项索引: {index}");
            return false;
        }

        UpgradeOptionSO option = pendingPayload.Choices[index];
        if (upgradeManager == null)
        {
            upgradeManager = ResolveUpgradeManager();
        }

        if (upgradeManager == null || !upgradeManager.TryApplyChoice(option, pendingPayload.Context.TriggerSource))
        {
            return false;
        }

        pendingPayload = null;
        upgradeManager?.ConsumeStatBuffCycleIfNeeded();
        gameFlowManager?.ConfirmUpgradeSelection();
        return true;
    }

    /// <summary>广告激励后重新抽取当前三选一候选。</summary>
    /// <returns>重抽成功返回 <c>true</c>。</returns>
    public bool TryRerollViaAd()
    {
        if (RemainingAdRerolls <= 0)
        {
            Debug.LogWarning("[RandomRewardManager] 本局广告刷新次数已用尽。");
            return false;
        }

        if (pendingPayload == null)
        {
            Debug.LogWarning("[RandomRewardManager] 当前没有可刷新的升级候选。");
            return false;
        }

        if (!RollAndPublish(pendingPayload.Context))
        {
            return false;
        }

        usedAdRerolls++;
        return true;
    }

    /// <summary>广告激励后应用全部候选并结束升级阶段。</summary>
    /// <returns>全选成功返回 <c>true</c>。</returns>
    public bool TrySelectAllViaAd()
    {
        if (RemainingAdSelectAll <= 0)
        {
            Debug.LogWarning("[RandomRewardManager] 本局广告全选次数已用尽。");
            return false;
        }

        if (pendingPayload == null || pendingPayload.Choices == null || pendingPayload.Choices.Count == 0)
        {
            Debug.LogWarning("[RandomRewardManager] 当前没有可全选的升级候选。");
            return false;
        }

        if (upgradeManager == null)
        {
            upgradeManager = ResolveUpgradeManager();
        }

        if (upgradeManager == null)
        {
            return false;
        }

        UpgradeTriggerSource source = pendingPayload.Context.TriggerSource;
        var choices = pendingPayload.Choices;
        for (int i = 0; i < choices.Count; i++)
        {
            UpgradeOptionSO option = choices[i];
            if (option == null)
            {
                continue;
            }

            if (!upgradeManager.TryApplyChoice(option, source))
            {
                Debug.LogWarning($"[RandomRewardManager] 全选应用失败: {option.ConfigId}");
            }
        }

        usedAdSelectAll++;
        pendingPayload = null;
        upgradeManager?.ConsumeStatBuffCycleIfNeeded();
        gameFlowManager?.ConfirmUpgradeSelection();
        return true;
    }

    /// <summary>新局开始时重置广告使用次数与待选 payload。</summary>
    /// <param name="ctx">游戏开始事件上下文。</param>
    private void OnGameStarted(GameEventContext ctx)
    {
        usedAdRerolls = 0;
        usedAdSelectAll = 0;
        pendingPayload = null;
    }

    /// <summary>调试：选择第一个候选。</summary>
    [ContextMenu("Debug/Select Choice 0")]
    private void DebugSelectChoice0() => TrySelectChoice(0);

    /// <summary>调试：选择第二个候选。</summary>
    [ContextMenu("Debug/Select Choice 1")]
    private void DebugSelectChoice1() => TrySelectChoice(1);

    /// <summary>调试：选择第三个候选。</summary>
    [ContextMenu("Debug/Select Choice 2")]
    private void DebugSelectChoice2() => TrySelectChoice(2);

    /// <summary>调试：重新抽取并发布候选列表。</summary>
    [ContextMenu("Debug/Roll Choices")]
    private void DebugRollChoices()
    {
        var context = BuildContext(UpgradeTriggerSource.Debug);
        RollAndPublish(context);
    }

    /// <summary>升级选择界面打开时抽取并发布候选。</summary>
    /// <param name="ctx">游戏事件上下文。</param>
    private void OnUpgradeSelectionOpened(GameEventContext ctx)
    {
        UpgradeTriggerSource source = UpgradeTriggerSource.WaveComplete;
        if (ServiceLocator.TryGet(out GameManager gameManager) &&
            gameManager.PreviousState == GameState.Playing)
        {
            source = UpgradeTriggerSource.LevelUp;
        }

        var context = new UpgradeSelectionContext(ResolveCurrentWave(), ResolvePlayerLevel(), source);
        if (upgradeManager == null)
        {
            upgradeManager = ResolveUpgradeManager();
        }

        upgradeManager?.NotifyUpgradeSelectionOpened(source);
        if (!RollAndPublish(context))
        {
            Debug.LogError("[RandomRewardManager] 无法生成升级候选，自动恢复战斗以免卡死。");
            gameFlowManager?.ConfirmUpgradeSelection();
        }
    }

    /// <summary>玩家升级时触发一次升级选择流程。</summary>
    /// <param name="ctx">游戏事件上下文。</param>
    /// <remarks>
    /// 升级节奏由 <see cref="PlayerController.GrantExperience"/> 保证“一次只升一级”，因此每个升级事件对应一次三选一；
    /// 这里仍保留状态守卫，防止在弹窗已开启时重复进入。
    /// </remarks>
    private void OnPlayerLevelUp(GameEventContext ctx)
    {
        if (gameFlowManager != null && gameFlowManager.IsAwaitingUpgradeSelection)
        {
            return;
        }

        if (!ServiceLocator.TryGet(out GameManager gameManager) ||
            gameManager.CurrentState != GameState.Playing)
        {
            return;
        }

        gameManager.BeginUpgradeChoosing();
    }

    /// <summary>抽取候选并发布 <see cref="GameEvents.RaiseUpgradeChoicesReady"/> 事件。</summary>
    /// <param name="context">升级抽取上下文。</param>
    /// <returns>抽取并发布成功返回 <c>true</c>。</returns>
    private bool RollAndPublish(UpgradeSelectionContext context)
    {
        if (upgradeManager == null)
        {
            upgradeManager = ResolveUpgradeManager();
        }

        if (upgradeManager == null)
        {
            Debug.LogError("[RandomRewardManager] UpgradeManager 未就绪。");
            return false;
        }

        if (!upgradeManager.TryRollChoices(context, out var choices))
        {
            return false;
        }

        string poolId = string.IsNullOrWhiteSpace(upgradeManager.LastResolvedPoolId)
            ? GameConstants.ConfigIds.RewardPoolDefault
            : upgradeManager.LastResolvedPoolId;
        pendingPayload = new UpgradeChoicesPayload(context, poolId);
        pendingPayload.SetChoices(choices);
        GameEvents.RaiseUpgradeChoicesReady(this, pendingPayload);

        if (autoConfirmFirstChoiceForDebug && choices.Count > 0)
        {
            TrySelectChoice(0);
        }

        return true;
    }

    /// <summary>构建升级抽取上下文。</summary>
    /// <param name="source">触发来源。</param>
    /// <returns>升级抽取上下文。</returns>
    private UpgradeSelectionContext BuildContext(UpgradeTriggerSource source)
    {
        int wave = ResolveCurrentWave();
        int level = ResolvePlayerLevel();
        return new UpgradeSelectionContext(wave, level, source);
    }

    /// <summary>解析当前波次索引。</summary>
    /// <returns>当前波次（最小为 1）。</returns>
    private static int ResolveCurrentWave()
    {
        if (ServiceLocator.TryGet(out SaveManager saveManager) &&
            saveManager.Current?.runProgress != null &&
            saveManager.Current.runProgress.hasActiveRun)
        {
            return Mathf.Max(1, saveManager.Current.runProgress.currentWave);
        }

        if (ServiceLocator.TryGet(out WaveManager waveManager) && waveManager.IsInitialized)
        {
            return Mathf.Max(1, waveManager.CurrentWaveIndex);
        }

        return 1;
    }

    /// <summary>解析当前玩家等级。</summary>
    /// <returns>当前等级（最小为 1）。</returns>
    private static int ResolvePlayerLevel()
    {
        if (PlayerSceneAccess.TryGetController(out PlayerController controller) &&
            controller.RuntimeStats.IsInitialized)
        {
            return Mathf.Max(1, controller.RuntimeStats.Data.CurrentLevel);
        }

        if (ServiceLocator.TryGet(out SaveManager saveManager) &&
            saveManager.Current?.runProgress != null)
        {
            return Mathf.Max(1, saveManager.Current.runProgress.currentLevel);
        }

        return 1;
    }

    /// <summary>从服务定位器或场景中解析 <see cref="UpgradeManager"/>。</summary>
    /// <returns>升级管理器实例；未找到时返回 <c>null</c>。</returns>
    private static UpgradeManager ResolveUpgradeManager()
    {
        if (ServiceLocator.TryGet(out UpgradeManager manager))
        {
            return manager;
        }

        return FindFirstObjectByType<UpgradeManager>();
    }
}
