using System;
using System.Collections.Generic;

/// <summary>
/// 可 JSON 序列化的配置 ID + 长整型值对（如 UTC Ticks）。
/// </summary>
[Serializable]
public struct ConfigIdLongPair
{
    public string configId;
    public long value;

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
