using UnityEngine;

/// <summary>
/// 同屏对象与特效/音效上限、移动端画质与敌人逻辑降频参数。
/// </summary>
/// <remarks>
/// <para><b>资产路径：</b><c>Assets/Resources/Config/Performance/PerformanceBudget_Default.asset</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "PerformanceBudget", menuName = "Attack Barbarians/Config/Performance Budget")]
public class PerformanceBudgetSO : ScriptableObject
{
    [Header("Alive Limits (0 = unlimited)")]
    [SerializeField] private int maxEnemiesAlive = 80;
    [SerializeField] private int maxProjectilesAlive = 120;
    [SerializeField] private int maxDamageNumbersAlive = 32;
    [SerializeField] private int maxCombatVfxAlive = 48;
    [SerializeField] private int maxConcurrentSfx = 12;

    [Header("Mobile Quality")]
    [SerializeField] private bool applyMobileProfileOnBootstrap = true;
    [Tooltip("移动端在玩家设置基础上的额外画质档位偏移（负值降档）。")]
    [SerializeField] private int mobileQualityLevelOffset = -1;
    [SerializeField] private int mobileTargetFrameRate = 60;
    [SerializeField, Range(0.25f, 1f)] private float mobileBudgetScale = 0.75f;

    [Header("Enemy Logic Throttle")]
    [Tooltip("距玩家超过该距离（世界单位）的敌人按间隔降频 Update。")]
    [SerializeField] private float enemyNearDistance = 18f;
    [SerializeField] private int enemyFarUpdateInterval = 3;

    [Header("Target Scan")]
    [SerializeField] private float targetScanIntervalIdle = 0.12f;
    [SerializeField] private float targetScanIntervalEngaged = 0.25f;

    /// <summary>同屏最大敌人数，0 表示不限制。</summary>
    public int MaxEnemiesAlive => Mathf.Max(0, maxEnemiesAlive);

    /// <summary>同屏最大投射物数，0 表示不限制。</summary>
    public int MaxProjectilesAlive => Mathf.Max(0, maxProjectilesAlive);

    /// <summary>同屏最大伤害飘字数，0 表示不限制。</summary>
    public int MaxDamageNumbersAlive => Mathf.Max(0, maxDamageNumbersAlive);

    /// <summary>同屏最大战斗特效数，0 表示不限制。</summary>
    public int MaxCombatVfxAlive => Mathf.Max(0, maxCombatVfxAlive);

    /// <summary>最大并发音效播放数（不小于 1）。</summary>
    public int MaxConcurrentSfx => Mathf.Max(1, maxConcurrentSfx);

    /// <summary>启动时是否应用移动端性能配置。</summary>
    public bool ApplyMobileProfileOnBootstrap => applyMobileProfileOnBootstrap;

    /// <summary>移动端画质档位偏移量（负值降档）。</summary>
    public int MobileQualityLevelOffset => mobileQualityLevelOffset;

    /// <summary>移动端目标帧率（不小于 30）。</summary>
    public int MobileTargetFrameRate => Mathf.Max(30, mobileTargetFrameRate);

    /// <summary>移动端预算缩放系数（0.25 ~ 1）。</summary>
    public float MobileBudgetScale => Mathf.Clamp(mobileBudgetScale, 0.25f, 1f);

    /// <summary>敌人视为"近距离"的距离阈值（世界单位）。</summary>
    public float EnemyNearDistance => Mathf.Max(1f, enemyNearDistance);

    /// <summary>远距离敌人的 Update 降频间隔（帧数）。</summary>
    public int EnemyFarUpdateInterval => Mathf.Max(1, enemyFarUpdateInterval);

    /// <summary>敌人近距离阈值的平方值，用于 sqrMagnitude 比较。</summary>
    public float EnemyNearDistanceSqr => EnemyNearDistance * EnemyNearDistance;

    /// <summary>空闲状态下目标扫描间隔（秒）。</summary>
    public float TargetScanIntervalIdle => Mathf.Max(0.05f, targetScanIntervalIdle);

    /// <summary>交战状态下目标扫描间隔（秒）。</summary>
    public float TargetScanIntervalEngaged => Mathf.Max(0.05f, targetScanIntervalEngaged);

    /// <summary>
    /// 获取指定预算分类的上限值，可按移动端缩放。
    /// </summary>
    /// <param name="category">预算分类。</param>
    /// <param name="useMobileScale">是否应用移动端预算缩放。</param>
    /// <returns>上限值，0 表示不限制。</returns>
    public int GetLimit(PerformanceBudgetCategory category, bool useMobileScale)
    {
        int raw = category switch
        {
            PerformanceBudgetCategory.Enemy => MaxEnemiesAlive,
            PerformanceBudgetCategory.Projectile => MaxProjectilesAlive,
            PerformanceBudgetCategory.DamageNumber => MaxDamageNumbersAlive,
            PerformanceBudgetCategory.CombatVfx => MaxCombatVfxAlive,
            _ => 0,
        };

        if (raw <= 0)
        {
            return 0;
        }

        if (!useMobileScale || MobileBudgetScale >= 0.999f)
        {
            return raw;
        }

        return Mathf.Max(1, Mathf.RoundToInt(raw * MobileBudgetScale));
    }
}
