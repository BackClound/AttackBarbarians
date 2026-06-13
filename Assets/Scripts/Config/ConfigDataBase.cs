using UnityEngine;

/// <summary>
/// 所有可配置 ScriptableObject 的基类，提供稳定 <see cref="ConfigId"/> 用于存档与查找。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject 资产）。</para>
/// <para><b>存放路径：</b>见 <c>docs/config_system.md</c> 命名规范。</para>
/// </remarks>
public abstract class ConfigDataBase : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string configId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    /// <summary>配置唯一标识，用于存档引用与运行时查找。</summary>
    public string ConfigId => configId;

    /// <summary>展示名称；未配置时回退为资产文件名。</summary>
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

    /// <summary>UI 展示用图标。</summary>
    public Sprite Icon => icon;

    /// <summary>
    /// 收集当前配置资产的校验错误与警告，由 <see cref="ConfigValidator"/> 调用，子类可追加规则。
    /// </summary>
    /// <param name="result">校验结果容器，用于写入错误与警告信息。</param>
    public virtual void CollectValidationErrors(ConfigValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(configId))
        {
            result.AddError(name, "configId 不能为空。");
        }
    }
}
