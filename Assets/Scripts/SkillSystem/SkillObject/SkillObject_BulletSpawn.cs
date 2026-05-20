using UnityEngine;

/// <summary>
/// 射击技能发射点：将目标与玩家攻击数值组装为 <see cref="ProjectileSpawnRequest"/> 并交给 <see cref="ProjectileManager"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 Player 射击波次子物体（如 <c>BulletSpawnPoint.prefab</c>）。</para>
/// <para><b>不再负责：</b>子弹列表预创建、碰撞伤害、对象池借还（已迁移至 Projectile 模块）。</para>
/// </remarks>
public class SkillObject_BulletSpawn : MonoBehaviour
{
    [Header("弹道")]
    [SerializeField] private int projectilesPerShot = 1;
    [SerializeField] private float fanAngleDegrees = 10f;
    [SerializeField] private ProjectileDataSO projectileData;

    private Player player;

    public void SetupBulletSpawn(Player owner, int bulletsPerShot)
    {
        player = owner;
        projectilesPerShot = Mathf.Max(1, bulletsPerShot);
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

        if (!ServiceLocator.TryGet(out ProjectileManager projectileManager))
        {
            Debug.LogWarning("[SkillObject_BulletSpawn] ProjectileManager 未注册，无法发射。");
            return;
        }

        float baseDamage = player.player_Health.entity_Stats.GetBaseAttackDamage();
        object source = player;
        Vector2 spawnPos = transform.position;
        Vector2 baseDirection = ((Vector2)enemy.transform.position - spawnPos).normalized;

        ProjectileSpawnRequest template = ProjectileSpawnRequest.CreateStraight(
            source,
            enemy.gameObject,
            spawnPos,
            baseDirection,
            baseDamage,
            GameConstants.ConfigIds.SkillShoot,
            ResolveProjectileData(projectileManager));

        if (projectilesPerShot <= 1)
        {
            projectileManager.Spawn(template);
            return;
        }

        ProjectileSpawnRequest fanTemplate = new ProjectileSpawnRequest(
            template.Source,
            template.Target,
            template.SpawnPosition,
            template.Direction,
            template.DamageInfo,
            template.Data,
            ProjectileSpawnPattern.Fan,
            projectilesPerShot,
            fanAngleDegrees,
            template.SkillId);

        projectileManager.SpawnFan(fanTemplate, projectilesPerShot, fanAngleDegrees);
    }

    private ProjectileDataSO ResolveProjectileData(ProjectileManager manager)
    {
        if (projectileData != null)
        {
            return projectileData;
        }

        return manager.DefaultData;
    }
}
