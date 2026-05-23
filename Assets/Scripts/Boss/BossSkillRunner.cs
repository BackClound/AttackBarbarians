using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss 技能冷却与释放（冲锋、召唤、范围攻击、护盾、弹幕）。
/// </summary>
/// <remarks>由 <see cref="BossController"/> 持有，无需单独挂载。</remarks>
public sealed class BossSkillRunner
{
    private const int MaxSkills = 8;

    private readonly BossSkillDataSO[] skills = new BossSkillDataSO[MaxSkills];
    private readonly float[] cooldownTimers = new float[MaxSkills];
    private readonly float[] activeTimers = new float[MaxSkills];

    private BossController owner;
    private EnemyController enemyController;
    private Enemy enemy;
    private EnemyStatusController statusController;
    private BossPhaseController phaseController;
    private int skillCount;
    private bool isActive;

    public void Initialize(
        BossController bossOwner,
        EnemyController controller,
        BossDataSO data,
        BossPhaseController phases)
    {
        owner = bossOwner;
        enemyController = controller;
        phaseController = phases;
        enemy = controller != null ? controller.Enemy : null;
        statusController = enemy != null ? enemy.GetComponent<EnemyStatusController>() : null;
        skillCount = 0;
        isActive = bossOwner != null && controller != null && data != null;

        for (int i = 0; i < MaxSkills; i++)
        {
            skills[i] = null;
            cooldownTimers[i] = 0f;
            activeTimers[i] = 0f;
        }

        if (!isActive)
        {
            return;
        }

        IReadOnlyList<string> skillIds = data.SkillConfigIds;
        if (skillIds == null || !ServiceLocator.TryGet(out ConfigManager configManager))
        {
            return;
        }

        for (int i = 0; i < skillIds.Count && skillCount < MaxSkills; i++)
        {
            string id = skillIds[i];
            if (string.IsNullOrEmpty(id) || !configManager.TryGetBossSkill(id, out BossSkillDataSO skill))
            {
                continue;
            }

            skills[skillCount] = skill;
            cooldownTimers[skillCount] = skill.CooldownSeconds * 0.5f;
            skillCount++;
        }
    }

    public void Tick(float deltaTime)
    {
        if (!isActive || skillCount == 0)
        {
            return;
        }

        int phaseIndex = phaseController != null ? phaseController.CurrentPhaseIndex : 0;

        for (int i = 0; i < skillCount; i++)
        {
            if (activeTimers[i] > 0f)
            {
                activeTimers[i] -= deltaTime;
                continue;
            }

            cooldownTimers[i] -= deltaTime;
            BossSkillDataSO skill = skills[i];
            if (skill == null || cooldownTimers[i] > 0f || phaseIndex < skill.MinPhaseIndex)
            {
                continue;
            }

            if (TryCast(skill))
            {
                cooldownTimers[i] = skill.CooldownSeconds;
                activeTimers[i] = skill.DurationSeconds;
            }
        }
    }

    public void Shutdown()
    {
        isActive = false;
        owner = null;
        enemyController = null;
        enemy = null;
        statusController = null;
        phaseController = null;
        skillCount = 0;
    }

    private bool TryCast(BossSkillDataSO skill)
    {
        if (skill == null || enemyController == null)
        {
            return false;
        }

        switch (skill.SkillType)
        {
            case BossSkillType.Charge:
                return ExecuteCharge(skill);
            case BossSkillType.Summon:
                return ExecuteSummon(skill);
            case BossSkillType.AreaAttack:
                return ExecuteAreaAttack(skill);
            case BossSkillType.Shield:
                return ExecuteShield(skill);
            case BossSkillType.Barrage:
                return ExecuteBarrage(skill);
            default:
                return false;
        }
    }

    private bool ExecuteCharge(BossSkillDataSO skill)
    {
        if (enemy == null)
        {
            return false;
        }

        float burstSpeed = enemy.moveSpeed * Mathf.Max(1f, skill.DamageMultiplier);
        enemy.SetVelocity(Vector2.down * burstSpeed);
        return true;
    }

    private bool ExecuteSummon(BossSkillDataSO skill)
    {
        if (string.IsNullOrEmpty(skill.SummonEnemyConfigId))
        {
            return false;
        }

        if (!ServiceLocator.TryGet(out EnemySpawnerManager spawner))
        {
            return false;
        }

        int waveIndex = 1;
        float multiplier = 1f;
        for (int i = 0; i < skill.SummonCount; i++)
        {
            spawner.TrySpawnEnemy(skill.SummonEnemyConfigId, multiplier, waveIndex);
        }

        return true;
    }

    private bool ExecuteAreaAttack(BossSkillDataSO skill)
    {
        float baseDamage = enemyController.GetMeleeDamage();
        float damage = baseDamage * skill.DamageMultiplier;
        enemyController.ExecuteWallAttack();
        TryBonusWallDamage(damage);
        return true;
    }

    private bool ExecuteShield(BossSkillDataSO skill)
    {
        if (statusController == null && enemy != null)
        {
            statusController = enemy.gameObject.AddComponent<EnemyStatusController>();
        }

        if (statusController != null)
        {
            statusController.ApplySlow(skill.DurationSeconds, 1f);
        }

        return true;
    }

    private bool ExecuteBarrage(BossSkillDataSO skill)
    {
        for (int i = 0; i < skill.HitCount; i++)
        {
            enemyController.ExecuteWallAttack();
        }

        return true;
    }

    private void TryBonusWallDamage(float damage)
    {
        if (enemy == null || damage <= 0f)
        {
            return;
        }

        if (ServiceLocator.TryGet(out WallControlManager wall))
        {
            wall.TakeDamageFromEnemy(enemy, damage);
        }
    }
}
