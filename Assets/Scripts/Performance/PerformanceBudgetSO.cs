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

    public int MaxEnemiesAlive => Mathf.Max(0, maxEnemiesAlive);
    public int MaxProjectilesAlive => Mathf.Max(0, maxProjectilesAlive);
    public int MaxDamageNumbersAlive => Mathf.Max(0, maxDamageNumbersAlive);
    public int MaxCombatVfxAlive => Mathf.Max(0, maxCombatVfxAlive);
    public int MaxConcurrentSfx => Mathf.Max(1, maxConcurrentSfx);

    public bool ApplyMobileProfileOnBootstrap => applyMobileProfileOnBootstrap;
    public int MobileQualityLevelOffset => mobileQualityLevelOffset;
    public int MobileTargetFrameRate => Mathf.Max(30, mobileTargetFrameRate);
    public float MobileBudgetScale => Mathf.Clamp(mobileBudgetScale, 0.25f, 1f);

    public float EnemyNearDistance => Mathf.Max(1f, enemyNearDistance);
    public int EnemyFarUpdateInterval => Mathf.Max(1, enemyFarUpdateInterval);
    public float EnemyNearDistanceSqr => EnemyNearDistance * EnemyNearDistance;

    public float TargetScanIntervalIdle => Mathf.Max(0.05f, targetScanIntervalIdle);
    public float TargetScanIntervalEngaged => Mathf.Max(0.05f, targetScanIntervalEngaged);

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
