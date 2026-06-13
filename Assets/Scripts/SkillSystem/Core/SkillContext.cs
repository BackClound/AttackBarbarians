using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能释放上下文：玩家、控制器、目标扫描缓冲与战斗工具引用；供 <see cref="ISkillEffect"/> 施法阶段使用。
/// </summary>
public sealed class SkillContext
{
    private static readonly Collider2D[] OverlapScratch = new Collider2D[48];

    private readonly List<Enemy> targetBuffer = new List<Enemy>(32);
    private readonly List<Enemy> chainBuffer = new List<Enemy>(16);
    private readonly HashSet<int> chainUsedIds = new HashSet<int>(16);

    /// <summary>施法玩家实体。</summary>
    public Player Player { get; }
    /// <summary>玩家控制器（目标扫描、属性读取）。</summary>
    public PlayerController Controller { get; }
    /// <summary>施法原点 Transform。</summary>
    public Transform CastOrigin { get; }
    /// <summary>所属技能管理器。</summary>
    public SkillManager SkillManager { get; }

    /// <summary>
    /// 构造技能上下文。
    /// </summary>
    /// <param name="player">玩家实体。</param>
    /// <param name="controller">玩家控制器。</param>
    /// <param name="manager">技能管理器。</param>
    /// <param name="castOrigin">施法原点；为空时回退到玩家 Transform。</param>
    public SkillContext(Player player, PlayerController controller, SkillManager manager, Transform castOrigin)
    {
        Player = player;
        Controller = controller;
        SkillManager = manager;
        CastOrigin = castOrigin != null ? castOrigin : player != null ? player.transform : null;
    }

    /// <summary>
    /// 获取技能基础伤害（优先玩家攻击力，否则配置默认值）。
    /// </summary>
    /// <param name="config">技能配置。</param>
    /// <returns>基础伤害值。</returns>
    public float GetBaseDamage(SkillDataSO config)
    {
        float fromConfig = config != null ? config.BaseDamage : 10f;
        if (Player != null && Player.player_Health != null && Player.player_Health.entity_Stats != null)
        {
            fromConfig = Player.player_Health.entity_Stats.GetBaseAttackDamage();
        }

        return fromConfig;
    }

    /// <summary>
    /// 获取攻击/扫描半径。
    /// </summary>
    /// <returns>攻击半径；未初始化时默认 25。</returns>
    public float GetAttackRadius()
    {
        if (Controller != null && Controller.RuntimeStats.IsInitialized)
        {
            return Controller.RuntimeStats.GetAttackRadius();
        }

        return 25f;
    }

    /// <summary>
    /// 获取主目标敌人（优先控制器主目标，否则战斗列表首项）。
    /// </summary>
    /// <param name="enemy">输出的主目标。</param>
    /// <returns>是否找到可受伤目标。</returns>
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

    /// <summary>
    /// 将当前战斗目标列表复制到指定缓冲区。
    /// </summary>
    /// <param name="destination">目标列表；调用方负责复用。</param>
    /// <returns>是否复制到至少一个目标。</returns>
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

    /// <summary>
    /// 从当前战斗目标列表中随机选取一个可受伤敌人（落雷/火雨等范围技能使用）。
    /// </summary>
    /// <param name="scratch">临时列表缓冲区。</param>
    /// <param name="enemy">随机选中的敌人。</param>
    /// <returns>是否成功选取。</returns>
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

    /// <summary>
    /// 获取攻击速度倍率。
    /// </summary>
    /// <returns>攻速倍率，下限 0.1。</returns>
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

    /// <summary>
    /// 从起始敌人出发，按最近邻链接构建链式目标列表（闪电链等）。
    /// </summary>
    /// <param name="start">链起点敌人。</param>
    /// <param name="maxCount">最大链接数量（含起点）。</param>
    /// <param name="maxLinkDistance">相邻链接最大距离。</param>
    /// <returns>内部复用的链式目标列表。</returns>
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

    /// <summary>
    /// 圆形范围查询可受伤敌人（落雷、火雨、水浪等 AoE 使用）。
    /// </summary>
    /// <param name="center">圆心世界坐标。</param>
    /// <param name="radius">查询半径。</param>
    /// <param name="results">结果写入列表；可为 null 仅返回计数。</param>
    /// <returns>命中的可受伤敌人数量。</returns>
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

    /// <summary>
    /// 构建标准 <see cref="DamageInfo"/>（含 Buff 伤害倍率）。
    /// </summary>
    /// <param name="runtime">技能运行时。</param>
    /// <param name="target">受击 GameObject。</param>
    /// <param name="skillMultiplier">额外技能伤害倍率，默认 1。</param>
    /// <returns>待提交伤害管道的伤害信息。</returns>
    public DamageInfo BuildDamageInfo(SkillRuntime runtime, GameObject target, float skillMultiplier = 1f)
    {
        SkillDataSO config = runtime.Config;
        float baseDamage = GetBaseDamage(config) * runtime.GetDamageMultiplier() * skillMultiplier;
        ElementType element = config != null ? config.ElementType : ElementType.None;
        string skillId = config != null ? config.ConfigId : string.Empty;
        return DamageInfo.Create(Player, target, baseDamage, 1f, skillId, element);
    }

    /// <summary>
    /// 通知控制器技能已施放（触发事件/UI/动画等）。
    /// </summary>
    /// <param name="runtime">已施放的技能运行时。</param>
    public void NotifyCast(SkillRuntime runtime)
    {
        if (Controller == null || runtime?.Config == null)
        {
            return;
        }

        Controller.NotifySkillCast(runtime.Config.ConfigId);
    }
}
