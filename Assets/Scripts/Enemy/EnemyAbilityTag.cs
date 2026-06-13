using System;

/// <summary>
/// 敌人能力标签（配置用 Flags，逻辑由能力组件实现）。
/// </summary>
/// <remarks><b>是否需要挂载：</b>否。</remarks>
[Flags]
public enum EnemyAbilityTag
{
    /// <summary>无特殊能力。</summary>
    None = 0,
    /// <summary>普通敌人。</summary>
    Normal = 1 << 0,
    /// <summary>冲锋：加速冲向目标。</summary>
    Charge = 1 << 1,
    /// <summary>护盾：吸收部分伤害。</summary>
    Shield = 1 << 2,
    /// <summary>分裂：死亡或受击时分裂为小怪。</summary>
    Split = 1 << 3,
    /// <summary>召唤：周期性召唤小怪。</summary>
    Summon = 1 << 4,
    /// <summary>远程：在射程外攻击。</summary>
    Ranged = 1 << 5,
    /// <summary>精英：额外属性缩放与奖励。</summary>
    Elite = 1 << 6,
}
