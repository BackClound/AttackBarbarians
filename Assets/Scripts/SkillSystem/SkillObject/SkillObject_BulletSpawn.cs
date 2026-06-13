using UnityEngine;

/// <summary>
/// 射击技能发射点（Legacy Prefab 兼容）：动画攻击帧回调时委托 <see cref="ShootProjectileCaster"/> 执行投射物生成。
/// 数据流：动画帧 → 本类 → <see cref="ShootProjectileCaster"/> → <see cref="ProjectileManager"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 Player 射击波次子物体（如 <c>BulletSpawnPoint.prefab</c>）。</para>
/// </remarks>
public class SkillObject_BulletSpawn : MonoBehaviour
{
    [Header("弹道")]
    [SerializeField] private int projectilesPerShot = 1;
    [SerializeField] private float fanAngleDegrees = 10f;
    [SerializeField] private ProjectileDataSO projectileData;

    private Player player;
    private ShootSkillController shootController;

    /// <summary>
    /// 初始化发射点所属玩家与每发弹道数。
    /// </summary>
    /// <param name="owner">所属玩家。</param>
    /// <param name="bulletsPerShot">每发弹道数。</param>
    public void SetupBulletSpawn(Player owner, int bulletsPerShot)
    {
        player = owner;
        projectilesPerShot = Mathf.Max(1, bulletsPerShot);
        shootController = owner != null ? owner.GetComponent<ShootSkillController>() : null;
    }

    /// <summary>
    /// 更新每发弹道数量（Legacy 升级回调）。
    /// </summary>
    /// <param name="newCount">新的弹道数。</param>
    public void UpdateMaxBullets(int newCount)
    {
        projectilesPerShot = Mathf.Max(1, newCount);
    }

    /// <summary>
    /// 向指定敌人发射投射物（动画攻击帧入口）。
    /// </summary>
    /// <param name="enemy">目标敌人。</param>
    public void ApplyBulletWithEnemy(Enemy enemy)
    {
        if (enemy == null || player == null)
        {
            return;
        }

        SkillManager skillManager = player.GetComponent<SkillManager>();
        if (skillManager == null || !skillManager.TryGetRuntime(SkillType.Shoot, out SkillRuntime runtime))
        {
            return;
        }

        SkillContext context = skillManager.Context;
        if (context == null)
        {
            return;
        }

        if (ShootProjectileCaster.TryFireAtEnemy(
                context,
                runtime,
                enemy,
                transform.position,
                fanAngleDegrees,
                ResolveProjectileData()))
        {
            context.NotifyCast(runtime);
        }
    }

    /// <summary>解析投射物配置（Inspector 覆盖优先，否则 ProjectileManager 默认值）。</summary>
    /// <returns>投射物数据资产。</returns>
    private ProjectileDataSO ResolveProjectileData()
    {
        if (projectileData != null)
        {
            return projectileData;
        }

        return ServiceLocator.TryGet(out ProjectileManager manager) ? manager.DefaultData : null;
    }
}
