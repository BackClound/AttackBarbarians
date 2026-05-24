using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 按 <see cref="EnemyDataSO"/> 能力标签与绑定表为敌人实例挂载能力组件。
/// </summary>
/// <remarks>纯静态工厂，无需挂载。</remarks>
public static class SpecialEnemyAbilityFactory
{
    public static void EnsureAbilities(EnemyController controller, EnemyDataSO data)
    {
        if (controller == null || data == null || !SpecialEnemyRules.HasMechanics(data.AbilityTags))
        {
            return;
        }

        IReadOnlyList<SpecialEnemyAbilityBinding> bindings = data.AbilityBindings;
        if (bindings != null && bindings.Count > 0)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                SpecialEnemyAbilityBinding binding = bindings[i];
                if (binding == null || binding.Tag == EnemyAbilityTag.None || binding.Tag == EnemyAbilityTag.Normal)
                {
                    continue;
                }

                if ((data.AbilityTags & binding.Tag) == 0)
                {
                    continue;
                }

                AttachAbility(controller.gameObject, binding.Tag, binding.AbilityConfigId);
            }

            return;
        }

        AttachAllTaggedAbilities(controller.gameObject, data.AbilityTags, data);
    }

    private static void AttachAllTaggedAbilities(GameObject instance, EnemyAbilityTag tags, EnemyDataSO data)
    {
        TryAttach(instance, EnemyAbilityTag.Charge, tags, data);
        TryAttach(instance, EnemyAbilityTag.Shield, tags, data);
        TryAttach(instance, EnemyAbilityTag.Split, tags, data);
        TryAttach(instance, EnemyAbilityTag.Summon, tags, data);
        TryAttach(instance, EnemyAbilityTag.Ranged, tags, data);
    }

    private static void TryAttach(GameObject instance, EnemyAbilityTag tag, EnemyAbilityTag tags, EnemyDataSO data)
    {
        if ((tags & tag) == 0)
        {
            return;
        }

        string configId = data.TryGetAbilityConfigId(tag);
        AttachAbility(instance, tag, configId);
    }

    private static void AttachAbility(GameObject instance, EnemyAbilityTag tag, string configId)
    {
        EnemyAbilityBase ability = GetOrAddAbilityComponent(instance, tag);
        if (ability == null)
        {
            return;
        }

        ability.Configure(configId);
    }

    private static EnemyAbilityBase GetOrAddAbilityComponent(GameObject instance, EnemyAbilityTag tag)
    {
        switch (tag)
        {
            case EnemyAbilityTag.Charge:
                return GetOrAdd<EnemyChargeAbility>(instance);
            case EnemyAbilityTag.Shield:
                return GetOrAdd<EnemyShieldAbility>(instance);
            case EnemyAbilityTag.Split:
                return GetOrAdd<EnemySplitAbility>(instance);
            case EnemyAbilityTag.Summon:
                return GetOrAdd<EnemySummonAbility>(instance);
            case EnemyAbilityTag.Ranged:
                return GetOrAdd<EnemyRangedAbility>(instance);
            default:
                return null;
        }
    }

    private static T GetOrAdd<T>(GameObject instance) where T : EnemyAbilityBase
    {
        if (instance.TryGetComponent(out T existing))
        {
            return existing;
        }

        return instance.AddComponent<T>();
    }
}
