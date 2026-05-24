using UnityEngine;

/// <summary>
/// 随机奖励调度：监听波次/升级事件，生成三选一候选并协调 UI 确认流程。
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

    private UpgradeManager upgradeManager;
    private GameFlowManager gameFlowManager;
    private UpgradeChoicesPayload pendingPayload;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;
    public UpgradeChoicesPayload PendingChoices => pendingPayload;

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
        isInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        GameEvents.UnsubscribeUpgradeSelectionOpened(OnUpgradeSelectionOpened);
        GameEvents.UnsubscribePlayerLevelUp(OnPlayerLevelUp);
        pendingPayload = null;
        isInitialized = false;
    }

    /// <summary>UI 或调试入口：选择第 index 个候选并结束升级阶段。</summary>
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
        gameFlowManager?.ConfirmUpgradeSelection();
        return true;
    }

    [ContextMenu("Debug/Select Choice 0")]
    private void DebugSelectChoice0() => TrySelectChoice(0);

    [ContextMenu("Debug/Select Choice 1")]
    private void DebugSelectChoice1() => TrySelectChoice(1);

    [ContextMenu("Debug/Select Choice 2")]
    private void DebugSelectChoice2() => TrySelectChoice(2);

    [ContextMenu("Debug/Roll Choices")]
    private void DebugRollChoices()
    {
        var context = BuildContext(UpgradeTriggerSource.Debug);
        RollAndPublish(context);
    }

    private void OnUpgradeSelectionOpened(GameEventContext ctx)
    {
        UpgradeTriggerSource source = UpgradeTriggerSource.WaveComplete;
        if (ServiceLocator.TryGet(out GameManager gameManager) &&
            gameManager.PreviousState == GameState.Playing)
        {
            source = UpgradeTriggerSource.LevelUp;
        }

        var context = new UpgradeSelectionContext(ResolveCurrentWave(), ResolvePlayerLevel(), source);
        RollAndPublish(context);
    }

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

    private UpgradeSelectionContext BuildContext(UpgradeTriggerSource source)
    {
        int wave = ResolveCurrentWave();
        int level = ResolvePlayerLevel();
        return new UpgradeSelectionContext(wave, level, source);
    }

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

    private static UpgradeManager ResolveUpgradeManager()
    {
        if (ServiceLocator.TryGet(out UpgradeManager manager))
        {
            return manager;
        }

        return FindFirstObjectByType<UpgradeManager>();
    }
}
