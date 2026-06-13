using System;
using UnityEngine;

/// <summary>
/// 敌人能力与配置表 Id 的绑定条目。
/// </summary>
[Serializable]
public class SpecialEnemyAbilityBinding
{
    [SerializeField] private EnemyAbilityTag tag = EnemyAbilityTag.Charge;
    [SerializeField] private string abilityConfigId;

    /// <summary>能力标签。</summary>
    public EnemyAbilityTag Tag => tag;

    /// <summary>绑定的特殊能力 configId。</summary>
    public string AbilityConfigId => abilityConfigId;
}
