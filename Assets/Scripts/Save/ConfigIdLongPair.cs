using System;
using System.Collections.Generic;

/// <summary>
/// 可 JSON 序列化的配置 ID + 长整型值对（如 UTC Ticks）。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。用于商店上次购买时间等长整型存档字段。</para>
/// </remarks>
[Serializable]
public struct ConfigIdLongPair
{
    /// <summary>配置唯一标识。</summary>
    public string configId;

    /// <summary>关联的长整型值（如 UTC Ticks）。</summary>
    public long value;

    /// <summary>
    /// 创建指定 configId 与长整型值的键值对。
    /// </summary>
    /// <param name="configId">配置唯一标识。</param>
    /// <param name="value">关联的长整型值（如 UTC Ticks）。</param>
    public ConfigIdLongPair(string configId, long value)
    {
        this.configId = configId ?? string.Empty;
        this.value = value;
    }
}

/// <summary>
/// <see cref="ConfigIdLongPair"/> 列表读写辅助。
/// </summary>
public static class ConfigIdLongPairListUtility
{
    /// <summary>
    /// 从列表中按 configId 读取长整型值。
    /// </summary>
    /// <param name="list">存档键值对列表。</param>
    /// <param name="configId">要查询的配置唯一标识。</param>
    /// <param name="defaultValue">未找到时返回的默认值。</param>
    /// <returns>匹配的长整型值，或默认值。</returns>
    public static long GetValue(IReadOnlyList<ConfigIdLongPair> list, string configId, long defaultValue = 0)
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
    /// 向列表写入或更新 configId 对应的长整型值；值为 0 时移除条目。
    /// </summary>
    /// <param name="list">存档键值对列表。</param>
    /// <param name="configId">配置唯一标识。</param>
    /// <param name="value">要写入的长整型值；为 0 时删除该条目。</param>
    public static void SetValue(List<ConfigIdLongPair> list, string configId, long value)
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
                list[i] = new ConfigIdLongPair(configId, value);
            }

            return;
        }

        if (value != 0)
        {
            list.Add(new ConfigIdLongPair(configId, value));
        }
    }
}
