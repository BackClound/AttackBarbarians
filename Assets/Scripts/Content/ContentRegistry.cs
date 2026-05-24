using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 内容注册表：统一登记敌人、技能、Boss、地图与局内事件配置，并提供集中校验入口。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 <c>GameSystems</c> 上。</para>
/// <para><b>获取方式：</b><c>ServiceLocator.Get&lt;ContentRegistry&gt;()</c>（Bootstrap 之后）。</para>
/// </remarks>
public class ContentRegistry : MonoBehaviour, IGameSystem
{
    private ConfigManager configManager;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;

    public void Initialize()
    {
        configManager = ServiceLocator.TryGet(out ConfigManager cm) ? cm : null;
        if (configManager == null)
        {
            Debug.LogError("[ContentRegistry] ConfigManager 未就绪，内容索引不可用。");
            isInitialized = false;
            return;
        }

        ConfigValidationResult contentValidation = ValidateContent(configManager.Database);
        if (!contentValidation.IsValid)
        {
            Debug.LogError($"[ContentRegistry] 内容校验失败:\n{contentValidation.BuildReport()}");
        }
        else if (configManager.ShouldLog())
        {
            Debug.Log("[ContentRegistry] 内容校验通过。");
        }

        isInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        configManager = null;
        isInitialized = false;
    }

    public bool TryGetMap(string configId, out MapDataSO data)
    {
        data = null;
        return configManager != null && configManager.TryGetMap(configId, out data);
    }

    public bool TryGetGameplayEvent(string configId, out GameplayEventDataSO data)
    {
        data = null;
        return configManager != null && configManager.TryGetGameplayEvent(configId, out data);
    }

    public bool TryGetEnemy(string configId, out EnemyDataSO data)
    {
        data = null;
        return configManager != null && configManager.TryGetEnemy(configId, out data);
    }

    public bool TryGetSkill(string configId, out SkillDataSO data)
    {
        data = null;
        return configManager != null && configManager.TryGetSkill(configId, out data);
    }

    public bool TryGetBoss(string configId, out BossDataSO data)
    {
        data = null;
        return configManager != null && configManager.TryGetBoss(configId, out data);
    }

    public bool TryGetBuff(string configId, out BuffDataSO data)
    {
        data = null;
        return configManager != null && configManager.TryGetBuff(configId, out data);
    }

    public static ConfigValidationResult ValidateContent(ConfigDatabaseSO database)
    {
        var result = new ConfigValidationResult();
        if (database == null)
        {
            result.AddError("ContentRegistry", "ConfigDatabase 为空。");
            return result;
        }

        result.Merge(ConfigValidator.ValidateDatabase(database));
        return result;
    }
}
