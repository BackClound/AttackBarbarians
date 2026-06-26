using UnityEngine;

/// <summary>
/// 蝙蝠敌人：默认敌人 Prefab 子类。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在蝙蝠敌人 Prefab 根节点，替代通用 <see cref="Enemy"/> 使用。</para>
/// </remarks>
public class BatEnemy : Enemy
{
    /// <summary>
    /// 获取近战攻击伤害（优先读控制器配置）。
    /// </summary>
    /// <returns>对城墙造成的伤害数值。</returns>
    public override float GetDamageValue() => GetMeleeDamageFromStats();

    /// <summary>
    /// 从控制器或 Entity_Stats 读取近战伤害。
    /// </summary>
    /// <returns>近战伤害值。</returns>
    private float GetMeleeDamageFromStats()
    {
        if (controller != null && controller.IsReady)
        {
            return controller.GetMeleeDamage();
        }

        if (enemy_Health != null && enemy_Health.entity_Stats != null)
        {
            return enemy_Health.entity_Stats.GetBaseAttackDamage();
        }

        return 10f;
    }
}
