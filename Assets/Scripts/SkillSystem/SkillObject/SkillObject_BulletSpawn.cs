using UnityEngine;

/// <summary>
/// 射击技能发射点（兼容 Prefab）：委托 <see cref="ShootProjectileCaster"/> 执行投射物生成。
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

    public void SetupBulletSpawn(Player owner, int bulletsPerShot)
    {
        player = owner;
        projectilesPerShot = Mathf.Max(1, bulletsPerShot);
        shootController = owner != null ? owner.GetComponent<ShootSkillController>() : null;
    }

    public void UpdateMaxBullets(int newCount)
    {
        projectilesPerShot = Mathf.Max(1, newCount);
    }

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

        ShootProjectileCaster.TryFireAtEnemy(
            context,
            runtime,
            enemy,
            transform.position,
            fanAngleDegrees,
            ResolveProjectileData());
    }

    private ProjectileDataSO ResolveProjectileData()
    {
        if (projectileData != null)
        {
            return projectileData;
        }

        return ServiceLocator.TryGet(out ProjectileManager manager) ? manager.DefaultData : null;
    }
}
