using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能释放上下文：玩家、控制器、扫描缓冲与战斗工具引用。
/// </summary>
public sealed class SkillContext
{
    private static readonly Collider2D[] OverlapScratch = new Collider2D[48];

    private readonly List<Enemy> targetBuffer = new List<Enemy>(32);
    private readonly List<Enemy> chainBuffer = new List<Enemy>(16);
    private readonly HashSet<int> chainUsedIds = new HashSet<int>(16);

    public Player Player { get; }
    public PlayerController Controller { get; }
    public Transform CastOrigin { get; }
    public SkillManager SkillManager { get; }

    public SkillContext(Player player, PlayerController controller, SkillManager manager, Transform castOrigin)
    {
        Player = player;
        Controller = controller;
        SkillManager = manager;
        CastOrigin = castOrigin != null ? castOrigin : player != null ? player.transform : null;
    }

    public float GetBaseDamage(SkillDataSO config)
    {
        float fromConfig = config != null ? config.BaseDamage : 10f;
        if (Player != null && Player.player_Health != null && Player.player_Health.entity_Stats != null)
        {
            fromConfig = Player.player_Health.entity_Stats.GetBaseAttackDamage();
        }

        return fromConfig;
    }

    public float GetAttackRadius()
    {
        if (Controller != null && Controller.RuntimeStats.IsInitialized)
        {
            return Controller.RuntimeStats.GetAttackRadius();
        }

        return 25f;
    }

    public bool TryGetPrimaryTarget(out Enemy enemy)
    {
        enemy = null;
        if (Controller != null && Controller.IsReady)
        {
            enemy = Controller.GetPrimaryTarget();
            if (enemy != null && enemy.enemy_Health != null && enemy.enemy_Health.CanBeDamage())
            {
                return true;
            }
        }

        return TryCopyTargets(targetBuffer) && targetBuffer.Count > 0 && (enemy = targetBuffer[0]) != null;
    }

    public bool TryCopyTargets(List<Enemy> destination)
    {
        if (destination == null)
        {
            return false;
        }

        destination.Clear();
        if (Controller != null && Controller.IsReady && Controller.CopyCombatTargetsTo(destination))
        {
            return destination.Count > 0;
        }

        return false;
    }

    /// <summary>从当前战斗目标列表中随机选取一个可受伤敌人（落雷/火雨等范围技能使用）。</summary>
    public bool TryPickRandomTarget(List<Enemy> scratch, out Enemy enemy)
    {
        enemy = null;
        if (!TryCopyTargets(scratch) || scratch.Count == 0)
        {
            return false;
        }

        enemy = scratch[Random.Range(0, scratch.Count)];
        if (enemy == null || enemy.enemy_Health == null || !enemy.enemy_Health.CanBeDamage())
        {
            enemy = null;
            return false;
        }

        return true;
    }

    public float GetAttackSpeedMultiplier()
    {
        if (Controller != null && Controller.RuntimeStats.IsInitialized)
        {
            return Mathf.Max(0.1f, Controller.RuntimeStats.Get(StatType.AttackSpeedMulti));
        }

        if (Player != null && Player.player_Health != null && Player.player_Health.entity_Stats != null)
        {
            return Mathf.Max(0.1f, Player.player_Health.entity_Stats.GetAttackSpeedMultiplier());
        }

        return 1f;
    }

    public List<Enemy> GetChainTargets(Enemy start, int maxCount, float maxLinkDistance)
    {
        chainBuffer.Clear();
        if (start == null || maxCount <= 0)
        {
            return chainBuffer;
        }

        TryCopyTargets(targetBuffer);
        chainBuffer.Add(start);
        Vector2 cursor = start.transform.position;
        chainUsedIds.Clear();
        chainUsedIds.Add(start.gameObject.GetInstanceID());

        while (chainBuffer.Count < maxCount && targetBuffer.Count > 0)
        {
            Enemy best = null;
            float bestDist = maxLinkDistance * maxLinkDistance;
            for (int i = 0; i < targetBuffer.Count; i++)
            {
                Enemy candidate = targetBuffer[i];
                if (candidate == null || candidate.enemy_Health == null || !candidate.enemy_Health.CanBeDamage())
                {
                    continue;
                }

                int id = candidate.gameObject.GetInstanceID();
                if (chainUsedIds.Contains(id))
                {
                    continue;
                }

                float sqr = ((Vector2)candidate.transform.position - cursor).sqrMagnitude;
                if (sqr < bestDist)
                {
                    bestDist = sqr;
                    best = candidate;
                }
            }

            if (best == null)
            {
                break;
            }

            chainBuffer.Add(best);
            chainUsedIds.Add(best.gameObject.GetInstanceID());
            cursor = best.transform.position;
        }

        return chainBuffer;
    }

    public int QueryEnemiesInCircle(Vector2 center, float radius, List<Enemy> results)
    {
        results?.Clear();
        LayerMask layers = Physics2D.DefaultRaycastLayers;
        if (ServiceLocator.TryGet(out CollisionManager collisionManager))
        {
            layers = collisionManager.GetPlayerEnemyScanLayers(0);
        }

        int count = CollisionQuery.OverlapCircleNonAlloc(center, radius, layers, OverlapScratch);
        int added = 0;
        for (int i = 0; i < count; i++)
        {
            if (!CollisionQuery.TryResolveEnemy(OverlapScratch[i], out Enemy enemy))
            {
                continue;
            }

            if (enemy.enemy_Health == null || !enemy.enemy_Health.CanBeDamage())
            {
                continue;
            }

            results?.Add(enemy);
            added++;
        }

        return added;
    }

    public DamageInfo BuildDamageInfo(SkillRuntime runtime, GameObject target, float skillMultiplier = 1f)
    {
        SkillDataSO config = runtime.Config;
        float baseDamage = GetBaseDamage(config) * runtime.GetDamageMultiplier() * skillMultiplier;
        ElementType element = config != null ? config.ElementType : ElementType.None;
        string skillId = config != null ? config.ConfigId : string.Empty;
        return DamageInfo.Create(Player, target, baseDamage, 1f, skillId, element);
    }

    public void NotifyCast(SkillRuntime runtime)
    {
        if (Controller == null || runtime?.Config == null)
        {
            return;
        }

        Controller.NotifySkillCast(runtime.Config.ConfigId);
    }
}
