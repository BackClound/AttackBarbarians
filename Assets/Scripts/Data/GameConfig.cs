using UnityEngine;

/// <summary>
/// 全局游戏配置（ScriptableObject），控制启动行为、日志与对象池默认策略。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject 资产，不是 MonoBehaviour）。</para>
/// <para><b>创建方式：</b>Project 窗口右键 → Create → Attack Barbarians → Config → Game Config。</para>
/// <para><b>存放路径：</b><c>Assets/Resources/Config/GameConfig.asset</c>（须与 <see cref="GameConstants.ResourcePaths.GameConfig"/> 一致，否则 <see cref="ConfigManager"/> 无法自动加载）。</para>
/// <para><b>引用方式：</b>在 <see cref="ConfigManager"/> 的 Inspector 中拖入；或由 Resources 自动加载。</para>
/// </remarks>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Attack Barbarians/Config/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Runtime")]
    [SerializeField] private bool startGameOnBootstrap = true;
    [SerializeField] private bool enableRuntimeLogs;

    [Header("Game Flow")]
    [Tooltip("Bootstrap 后跳过升级三选一，波次结算后直接进入 Playing。")]
    [SerializeField] private bool skipUpgradeChoosingOnBootstrap;
    [SerializeField] private float defaultWaveTransitionSeconds = 1.5f;

    [Header("Config")]
    [SerializeField] private ConfigDatabaseSO configDatabase;
    [SerializeField] private bool validateConfigOnBootstrap = true;
    [SerializeField] private string defaultMapConfigId = GameConstants.ConfigIds.MapDefault;
    [SerializeField] private EliteModeConfigSO eliteModeConfig;
    [Tooltip("调试：开局启用精英模式全局倍率。")]
    [SerializeField] private bool startWithEliteMode;

    [Header("Pool")]
    [SerializeField] private int defaultPoolPrewarmCount = 8;
    [SerializeField] private bool allowPoolGrowth = true;

    [Header("Performance")]
    [SerializeField] private PerformanceBudgetSO performanceBudget;

    [Header("Save")]
    [SerializeField] private string saveFileName = SaveConstants.DefaultSaveFileName;
    [SerializeField] private bool enableAutoSave = true;
    [SerializeField] private float autoSaveDebounceSeconds = 2f;

    /// <summary>Bootstrap 完成后是否自动调用 <see cref="GameManager.StartGame"/>。</summary>
    public bool StartGameOnBootstrap => startGameOnBootstrap;
    public bool EnableRuntimeLogs => enableRuntimeLogs;
    /// <summary>Bootstrap 后是否跳过升级三选一。</summary>
    public bool SkipUpgradeChoosingOnBootstrap => skipUpgradeChoosingOnBootstrap;
    /// <summary>波次切换 UI 默认展示秒数。</summary>
    public float DefaultWaveTransitionSeconds => Mathf.Max(0f, defaultWaveTransitionSeconds);
    /// <summary>全局配置数据库引用。</summary>
    public ConfigDatabaseSO ConfigDatabase => configDatabase;
    /// <summary>Bootstrap 时是否校验配置完整性。</summary>
    public bool ValidateConfigOnBootstrap => validateConfigOnBootstrap;
    /// <summary>默认地图配置 ID。</summary>
    public string DefaultMapConfigId => string.IsNullOrWhiteSpace(defaultMapConfigId)
        ? GameConstants.ConfigIds.MapDefault
        : defaultMapConfigId;
    /// <summary>精英模式全局倍率配置。</summary>
    public EliteModeConfigSO EliteModeConfig => eliteModeConfig;
    /// <summary>调试：开局是否启用精英模式。</summary>
    public bool StartWithEliteMode => startWithEliteMode;
    /// <summary>对象池默认预热数量。</summary>
    public int DefaultPoolPrewarmCount => Mathf.Max(0, defaultPoolPrewarmCount);
    /// <summary>对象池是否允许运行时扩容。</summary>
    public bool AllowPoolGrowth => allowPoolGrowth;
    /// <summary>性能预算配置（帧率、实体上限等）。</summary>
    public PerformanceBudgetSO PerformanceBudget => performanceBudget;
    /// <summary>存档文件名。</summary>
    public string SaveFileName => string.IsNullOrWhiteSpace(saveFileName)
        ? SaveConstants.DefaultSaveFileName
        : saveFileName;
    /// <summary>是否启用自动存档。</summary>
    public bool EnableAutoSave => enableAutoSave;
    /// <summary>自动存档防抖间隔（秒）。</summary>
    public float AutoSaveDebounceSeconds => Mathf.Max(0.1f, autoSaveDebounceSeconds);
}
