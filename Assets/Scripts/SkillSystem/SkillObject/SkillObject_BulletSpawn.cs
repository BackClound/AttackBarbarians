using UnityEngine;

/// <summary>
/// 射击技能发射点：将目标与玩家攻击数值组装为 <see cref="ProjectileSpawnRequest"/> 并交给 <see cref="ProjectileManager"/>。
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
    private SkillManager skillManager;

    public void SetupBulletSpawn(Player owner, int bulletsPerShot)
    {
        player = owner;
        projectilesPerShot = Mathf.Max(1, bulletsPerShot);
        skillManager = owner != null ? owner.GetComponent<SkillManager>() : null;
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

        SkillBuffProfile buff = skillManager != null
            ? skillManager.GetBuffProfile(SkillType.Shoot)
            : null;

        int trajectoryLines = buff != null ? Mathf.Max(1, buff.TrajectoryLines) : 1;
        int volley = buff != null ? Mathf.Max(1, buff.ShotsPerVolley) : projectilesPerShot;
        float fan = trajectoryLines > 1 ? fanAngleDegrees : 0f;

        float baseDamage = player.player_Health.entity_Stats.GetBaseAttackDamage();
        if (buff != null)
        {
            baseDamage *= buff.DamageMultiplier;
        }

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

        ProjectileRuntimeOverrides overrides = buff != null
            ? ProjectileRuntimeOverrides.FromShootProfile(buff)
            : default;

        int total = trajectoryLines * volley;
        if (total <= 1)
        {
            projectileManager.Spawn(template, overrides);
            return;
        }

        int middle = total / 2;
        for (int i = 0; i < total; i++)
        {
            float angleOffset = (i - middle) * fan;
            Vector2 dir = Rotate(baseDirection, angleOffset);
            projectileManager.Spawn(template.WithDirection(dir), overrides);
        }
    }

    private ProjectileDataSO ResolveProjectileData(ProjectileManager manager)
    {
        if (projectileData != null)
        {
            return projectileData;
        }

        return manager.DefaultData;
    }

    private static Vector2 Rotate(Vector2 direction, float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos).normalized;
    }
}
