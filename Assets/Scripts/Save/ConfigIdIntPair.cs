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
    public string configId;
    public int value;

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
