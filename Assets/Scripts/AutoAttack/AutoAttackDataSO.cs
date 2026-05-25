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

    public string SkillId => string.IsNullOrEmpty(skillId) ? GameConstants.ConfigIds.SkillShoot : skillId;
    public int ShotsPerBurst => Mathf.Max(1, shotsPerBurst);
    public float BurstRecoverySeconds => Mathf.Max(0f, burstRecoverySeconds);
    public float ScanIntervalWhileIdle => Mathf.Max(0.05f, scanIntervalWhileIdle);
    public float ScanIntervalWhileEngaged => Mathf.Max(0.05f, scanIntervalWhileEngaged);
    public ProjectileDataSO ProjectileData => projectileData;
    public int ProjectilesPerShot => Mathf.Max(1, projectilesPerShot);
    public float FanAngleDegrees => fanAngleDegrees;
    public ProjectileSpawnPattern SpawnPattern => spawnPattern;

    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (shotsPerBurst < 1)
        {
            result.AddError(name, "shotsPerBurst 必须 >= 1。");
        }
    }
}
