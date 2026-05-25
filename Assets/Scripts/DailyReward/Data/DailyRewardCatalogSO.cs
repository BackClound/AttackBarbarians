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

    public IReadOnlyList<DailyRewardEntrySO> Entries => entries;
    public bool AllowMakeup => allowMakeup;
    public long MakeupDiamondCost => (long)Mathf.Max(0, makeupDiamondCost);
    public int MaxDay => 7;

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
