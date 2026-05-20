using System;

/// <summary>
/// 敌人能力标签（配置用 Flags，逻辑由能力组件实现）。
/// </summary>
[Flags]
public enum EnemyAbilityTag
{
    None = 0,
    // 普通
    Normal = 1 << 0,
    // 冲锋
    Charge = 1 << 1,
    // 护盾
    Shield = 1 << 2,
    // 分裂
    Split = 1 << 3,
    // 召唤
    Summon = 1 << 4,
    // 远程
    Ranged = 1 << 5,
    // 精英
    Elite = 1 << 6,
}
