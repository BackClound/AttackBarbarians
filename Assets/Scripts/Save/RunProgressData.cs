using System;
using System.Collections.Generic;

/// <summary>
/// 局内临时进度：波次、等级、血量、已选 Buff 等（可选持久化，防崩溃丢档）。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。嵌套在 <see cref="SaveData"/> 中。</para>
/// <para><b>Buff 列表：</b>仅存 <c>configId</c> + stacks（<see cref="ConfigIdIntPair.value"/>）。</para>
/// </remarks>
[Serializable]
public class RunProgressData
{
    public bool hasActiveRun;
    public int currentWave;
    public int currentLevel;
    public float currentHp;
    public float maxHp;
    public List<ConfigIdIntPair> activeBuffs = new List<ConfigIdIntPair>(8);

    public static RunProgressData CreateDefault() => new RunProgressData();

    public void ClearRun()
    {
        hasActiveRun = false;
        currentWave = 0;
        currentLevel = 1;
        currentHp = 0f;
        maxHp = 0f;
        activeBuffs?.Clear();
    }
}
