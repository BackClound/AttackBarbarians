using System;
using System.Collections.Generic;

/// <summary>
/// 可 JSON 序列化的配置 ID + 整数值对，用于技能等级、永久升级、Buff 层数等。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。仅作为 <see cref="SaveData"/> 内列表元素。</para>
/// <para><b>约定：</b>只存 <c>configId</c>，不存 Unity 对象引用。</para>
/// </remarks>
[Serializable]
public struct ConfigIdIntPair
{
    /// <summary>配置唯一标识。</summary>
    public string configId;

    /// <summary>关联的整数值（等级、层数或数量等）。</summary>
    public int value;

    /// <summary>
    /// 创建指定 configId 与整数值的键值对。
    /// </summary>
    /// <param name="configId">配置唯一标识。</param>
    /// <param name="value">关联的整数值（等级、层数或数量等）。</param>
    public ConfigIdIntPair(string configId, int value)
    {
        this.configId = configId ?? string.Empty;
        this.value = value;
    }
}

/// <summary>
/// <see cref="ConfigIdIntPair"/> 列表读写辅助，避免 JsonUtility 无法序列化 Dictionary。
/// </summary>
public static class ConfigIdIntPairListUtility
{
    /// <summary>
    /// 从列表中按 configId 读取整数值。
    /// </summary>
    /// <param name="list">存档键值对列表。</param>
    /// <param name="configId">要查询的配置唯一标识。</param>
    /// <param name="defaultValue">未找到时返回的默认值。</param>
    /// <returns>匹配的整数值，或默认值。</returns>
    public static int GetValue(IReadOnlyList<ConfigIdIntPair> list, string configId, int defaultValue = 0)
    {
        if (list == null || string.IsNullOrWhiteSpace(configId))
        {
            return defaultValue;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].configId == configId)
            {
                return list[i].value;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// 向列表写入或更新 configId 对应的整数值；值为 0 时移除条目。
    /// </summary>
    /// <param name="list">存档键值对列表。</param>
    /// <param name="configId">配置唯一标识。</param>
    /// <param name="value">要写入的整数值；为 0 时删除该条目。</param>
    public static void SetValue(List<ConfigIdIntPair> list, string configId, int value)
    {
        if (list == null || string.IsNullOrWhiteSpace(configId))
        {
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].configId != configId)
            {
                continue;
            }

            if (value == 0)
            {
                list.RemoveAt(i);
            }
            else
            {
                list[i] = new ConfigIdIntPair(configId, value);
            }

            return;
        }

        if (value != 0)
        {
            list.Add(new ConfigIdIntPair(configId, value));
        }
    }
}
