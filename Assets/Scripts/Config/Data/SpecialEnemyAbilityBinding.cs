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

    public EnemyAbilityTag Tag => tag;
    public string AbilityConfigId => abilityConfigId;
}
