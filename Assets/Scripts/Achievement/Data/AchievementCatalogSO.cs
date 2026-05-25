using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 成就目录：集中引用全部成就配置。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject 资产）。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/Achievement/AchievementCatalog_Default.asset</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "AchievementCatalog", menuName = "Attack Barbarians/Achievement/Achievement Catalog")]
public class AchievementCatalogSO : ScriptableObject
{
    [SerializeField] private List<AchievementDataSO> achievements = new List<AchievementDataSO>(16);

    public IReadOnlyList<AchievementDataSO> Achievements => achievements;

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
