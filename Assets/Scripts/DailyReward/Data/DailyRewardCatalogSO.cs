using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 七日签到目录与补签规则。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject 资产）。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/DailyReward/DailyRewardCatalog_Default.asset</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "DailyRewardCatalog", menuName = "Attack Barbarians/Daily Reward/Daily Reward Catalog")]
public class DailyRewardCatalogSO : ScriptableObject
{
    [SerializeField] private List<DailyRewardEntrySO> entries = new List<DailyRewardEntrySO>(7);

    [Header("Makeup")]
    [SerializeField] private bool allowMakeup = true;
    [SerializeField] private long makeupDiamondCost = 10;

    /// <summary>全部签到奖励条目。</summary>
    public IReadOnlyList<DailyRewardEntrySO> Entries => entries;
    /// <summary>是否允许补签。</summary>
    public bool AllowMakeup => allowMakeup;
    /// <summary>单次补签消耗的钻石数量。</summary>
    public long MakeupDiamondCost => (long)Mathf.Max(0, makeupDiamondCost);
    /// <summary>签到周期最大天数。</summary>
    public int MaxDay => 7;

    /// <summary>
    /// 按天数索引查找签到奖励条目。
    /// </summary>
    /// <param name="dayIndex">签到天数索引（1～7）。</param>
    /// <param name="entry">找到的奖励配置；未找到时为 null。</param>
    /// <returns>找到对应配置时返回 true。</returns>
    public bool TryGetEntry(int dayIndex, out DailyRewardEntrySO entry)
    {
        entry = null;
        if (entries == null || dayIndex < 1)
        {
            return false;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            DailyRewardEntrySO candidate = entries[i];
            if (candidate != null && candidate.DayIndex == dayIndex)
            {
                entry = candidate;
                return true;
            }
        }

        return false;
    }
}
