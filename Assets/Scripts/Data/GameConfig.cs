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

    [Header("Pool")]
    [SerializeField] private int defaultPoolPrewarmCount = 8;
    [SerializeField] private bool allowPoolGrowth = true;

    public bool StartGameOnBootstrap => startGameOnBootstrap;
    public bool EnableRuntimeLogs => enableRuntimeLogs;
    public int DefaultPoolPrewarmCount => Mathf.Max(0, defaultPoolPrewarmCount);
    public bool AllowPoolGrowth => allowPoolGrowth;
}
