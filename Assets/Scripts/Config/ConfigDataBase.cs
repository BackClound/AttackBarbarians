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

    public string ConfigId => configId;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public Sprite Icon => icon;

    /// <summary>由 <see cref="ConfigValidator"/> 调用，子类可追加规则。</summary>
    public virtual void CollectValidationErrors(ConfigValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(configId))
        {
            result.AddError(name, "configId 不能为空。");
        }
    }
}
