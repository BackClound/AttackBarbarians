using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 按 <see cref="EnemyDataSO"/> 能力标签与绑定表为敌人实例挂载能力组件。
/// </summary>
/// <remarks>纯静态工厂，无需挂载。</remarks>
public static class SpecialEnemyAbilityFactory
{
    /// <summary>为敌人实例确保挂载所有应激活的能力组件。</summary>
    /// <param name="controller">敌人控制器。</param>
    /// <param name="data">敌人配置。</param>
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

    /// <summary>按标签位逐个挂载默认能力。</summary>
    /// <param name="instance">敌人 GameObject。</param>
    /// <param name="tags">能力标签组合。</param>
    /// <param name="data">敌人配置。</param>
    private static void AttachAllTaggedAbilities(GameObject instance, EnemyAbilityTag tags, EnemyDataSO data)
    {
        TryAttach(instance, EnemyAbilityTag.Charge, tags, data);
        TryAttach(instance, EnemyAbilityTag.Shield, tags, data);
        TryAttach(instance, EnemyAbilityTag.Split, tags, data);
        TryAttach(instance, EnemyAbilityTag.Summon, tags, data);
        TryAttach(instance, EnemyAbilityTag.Ranged, tags, data);
    }

    /// <summary>若标签命中则挂载对应能力。</summary>
    /// <param name="instance">敌人 GameObject。</param>
    /// <param name="tag">目标能力标签。</param>
    /// <param name="tags">敌人全部标签。</param>
    /// <param name="data">敌人配置。</param>
    private static void TryAttach(GameObject instance, EnemyAbilityTag tag, EnemyAbilityTag tags, EnemyDataSO data)
    {
        if ((tags & tag) == 0)
        {
            return;
        }

        string configId = data.TryGetAbilityConfigId(tag);
        AttachAbility(instance, tag, configId);
    }

    /// <summary>获取或添加能力组件并完成配置。</summary>
    /// <param name="instance">敌人 GameObject。</param>
    /// <param name="tag">能力标签。</param>
    /// <param name="configId">能力配置 Id。</param>
    private static void AttachAbility(GameObject instance, EnemyAbilityTag tag, string configId)
    {
        EnemyAbilityBase ability = GetOrAddAbilityComponent(instance, tag);
        if (ability == null)
        {
            return;
        }

        ability.Configure(configId);
    }

    /// <summary>按标签类型获取或添加能力 MonoBehaviour。</summary>
    /// <param name="instance">敌人 GameObject。</param>
    /// <param name="tag">能力标签。</param>
    /// <returns>能力组件；不支持时返回 null。</returns>
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

    /// <summary>获取已有组件或添加新组件。</summary>
    /// <typeparam name="T">能力组件类型。</typeparam>
    /// <param name="instance">敌人 GameObject。</param>
    /// <returns>能力组件实例。</returns>
    private static T GetOrAdd<T>(GameObject instance) where T : EnemyAbilityBase
    {
        if (instance.TryGetComponent(out T existing))
        {
            return existing;
        }

        return instance.AddComponent<T>();
    }
}
