using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Meta 成就目录配置：集中引用全部成就条目，供 <see cref="AchievementManager"/> 加载。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject 资产）。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/Achievement/AchievementCatalog_Default.asset</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "AchievementCatalog", menuName = "Attack Barbarians/Achievement/Achievement Catalog")]
public class AchievementCatalogSO : ScriptableObject
{
    [SerializeField] private List<AchievementDataSO> achievements = new List<AchievementDataSO>(16);

    /// <summary>目录中全部成就配置条目。</summary>
    public IReadOnlyList<AchievementDataSO> Achievements => achievements;

    /// <summary>
    /// 按配置 ID 查找成就条目。
    /// </summary>
    /// <param name="configId">成就配置 ID。</param>
    /// <param name="achievement">找到的成就配置；未找到时为 null。</param>
    /// <returns>找到对应配置时返回 true。</returns>
    public bool TryGetAchievement(string configId, out AchievementDataSO achievement)
    {
        achievement = null;
        if (achievements == null || string.IsNullOrWhiteSpace(configId))
        {
            return false;
        }

        for (int i = 0; i < achievements.Count; i++)
        {
            AchievementDataSO entry = achievements[i];
            if (entry != null && entry.ConfigId == configId)
            {
                achievement = entry;
                return true;
            }
        }

        return false;
    }
}
