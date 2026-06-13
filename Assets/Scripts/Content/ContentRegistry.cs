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

    /// <summary>注册表是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;

    /// <summary>
    /// 绑定 ConfigManager 并执行内容校验。
    /// </summary>
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

    /// <summary>
    /// 每帧更新（当前无逻辑）。
    /// </summary>
    /// <param name="deltaTime">帧间隔时间（秒）。</param>
    public void Tick(float deltaTime) { }

    /// <summary>
    /// 释放 ConfigManager 引用并重置初始化状态。
    /// </summary>
    public void Shutdown()
    {
        configManager = null;
        isInitialized = false;
    }

    /// <summary>
    /// 按配置 ID 查找地图数据。
    /// </summary>
    /// <param name="configId">地图配置 ID。</param>
    /// <param name="data">找到时输出的地图数据。</param>
    /// <returns>找到返回 true，否则 false。</returns>
    public bool TryGetMap(string configId, out MapDataSO data)
    {
        data = null;
        return configManager != null && configManager.TryGetMap(configId, out data);
    }

    /// <summary>
    /// 按配置 ID 查找局内事件数据。
    /// </summary>
    /// <param name="configId">事件配置 ID。</param>
    /// <param name="data">找到时输出的事件数据。</param>
    /// <returns>找到返回 true，否则 false。</returns>
    public bool TryGetGameplayEvent(string configId, out GameplayEventDataSO data)
    {
        data = null;
        return configManager != null && configManager.TryGetGameplayEvent(configId, out data);
    }

    /// <summary>
    /// 按配置 ID 查找敌人数据。
    /// </summary>
    /// <param name="configId">敌人配置 ID。</param>
    /// <param name="data">找到时输出的敌人数据。</param>
    /// <returns>找到返回 true，否则 false。</returns>
    public bool TryGetEnemy(string configId, out EnemyDataSO data)
    {
        data = null;
        return configManager != null && configManager.TryGetEnemy(configId, out data);
    }

    /// <summary>
    /// 按配置 ID 查找技能数据。
    /// </summary>
    /// <param name="configId">技能配置 ID。</param>
    /// <param name="data">找到时输出的技能数据。</param>
    /// <returns>找到返回 true，否则 false。</returns>
    public bool TryGetSkill(string configId, out SkillDataSO data)
    {
        data = null;
        return configManager != null && configManager.TryGetSkill(configId, out data);
    }

    /// <summary>
    /// 按配置 ID 查找 Boss 数据。
    /// </summary>
    /// <param name="configId">Boss 配置 ID。</param>
    /// <param name="data">找到时输出的 Boss 数据。</param>
    /// <returns>找到返回 true，否则 false。</returns>
    public bool TryGetBoss(string configId, out BossDataSO data)
    {
        data = null;
        return configManager != null && configManager.TryGetBoss(configId, out data);
    }

    /// <summary>
    /// 按配置 ID 查找 Buff 数据。
    /// </summary>
    /// <param name="configId">Buff 配置 ID。</param>
    /// <param name="data">找到时输出的 Buff 数据。</param>
    /// <returns>找到返回 true，否则 false。</returns>
    public bool TryGetBuff(string configId, out BuffDataSO data)
    {
        data = null;
        return configManager != null && configManager.TryGetBuff(configId, out data);
    }

    /// <summary>
    /// 对配置数据库执行集中内容校验。
    /// </summary>
    /// <param name="database">配置数据库资产。</param>
    /// <returns>校验结果，包含错误与警告。</returns>
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
