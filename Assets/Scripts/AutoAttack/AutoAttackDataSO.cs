using UnityEngine;

/// <summary>
/// 自动攻击节拍配置：扫描间隔与连发休整（发弹参数以 <see cref="SkillDataSO"/> 为准）。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject）。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/AutoAttack/</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "AutoAttackData", menuName = "Attack Barbarians/Config/Auto Attack Data")]
public class AutoAttackDataSO : ConfigDataBase
{
    [Header("Identity")]
    [SerializeField] private string skillId = GameConstants.ConfigIds.SkillShoot;

    [Header("Timing")]
    [Tooltip("单轮连发内的最大射击次数，耗尽后进入 burstRecoverySeconds。")]
    [SerializeField] private int shotsPerBurst = 10;
    [SerializeField] private float burstRecoverySeconds = 4f;
    [Tooltip("无目标时的扫描间隔（秒）。")]
    [SerializeField] private float scanIntervalWhileIdle = 0.12f;
    [Tooltip("持有目标时的扫描间隔（秒）。")]
    [SerializeField] private float scanIntervalWhileEngaged = 0.25f;

    [Header("Projectile")]
    [SerializeField] private ProjectileDataSO projectileData;
    [SerializeField] private int projectilesPerShot = 1;
    [SerializeField] private float fanAngleDegrees = 10f;
    [SerializeField] private ProjectileSpawnPattern spawnPattern = ProjectileSpawnPattern.Single;

    /// <summary>关联的射击技能 Id。</summary>
    public string SkillId => string.IsNullOrEmpty(skillId) ? GameConstants.ConfigIds.SkillShoot : skillId;
    /// <summary>单轮连发最大射击次数。</summary>
    public int ShotsPerBurst => Mathf.Max(1, shotsPerBurst);
    /// <summary>连发耗尽后的休整时间（秒）。</summary>
    public float BurstRecoverySeconds => Mathf.Max(0f, burstRecoverySeconds);
    /// <summary>无目标时的目标扫描间隔（秒）。</summary>
    public float ScanIntervalWhileIdle => Mathf.Max(0.05f, scanIntervalWhileIdle);
    /// <summary>持有目标时的扫描间隔（秒）。</summary>
    public float ScanIntervalWhileEngaged => Mathf.Max(0.05f, scanIntervalWhileEngaged);
    /// <summary>默认投射物配置。</summary>
    public ProjectileDataSO ProjectileData => projectileData;
    /// <summary>每次射击的弹道数量。</summary>
    public int ProjectilesPerShot => Mathf.Max(1, projectilesPerShot);
    /// <summary>扇形排布夹角（度）。</summary>
    public float FanAngleDegrees => fanAngleDegrees;
    /// <summary>弹道排布模式。</summary>
    public ProjectileSpawnPattern SpawnPattern => spawnPattern;

    /// <summary>
    /// 收集配置校验错误。
    /// </summary>
    /// <param name="result">校验结果收集器。</param>
    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (shotsPerBurst < 1)
        {
            result.AddError(name, "shotsPerBurst 必须 >= 1。");
        }
    }
}
