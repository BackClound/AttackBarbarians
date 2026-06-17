using UnityEngine;

/// <summary>
/// 开局施加局外永久 SkillBuff（<see cref="SaveData.permanentUpgrades"/> 中带 skillBuff 的 BuffData）。
/// 与 <see cref="PlaytestBootstrap"/> 互补：后者处理 GameConfig 试玩 Buff，本类处理存档永久成长。
/// </summary>
internal static class MetaProgressBuffBootstrap
{
    /// <summary>从存档 permanentUpgrades 向玩家施加永久 SkillBuff。</summary>
    public static bool TryApplyPermanentSkillBuffs(SaveManager saveManager, BuffManager buffManager)
    {
        if (saveManager?.Current?.permanentUpgrades == null ||
            saveManager.Current.permanentUpgrades.Count == 0 ||
            buffManager == null)
        {
            return true;
        }

        if (!ServiceLocator.TryGet(out ConfigManager configManager))
        {
            return false;
        }

        int applied = 0;
        for (int i = 0; i < saveManager.Current.permanentUpgrades.Count; i++)
        {
            ConfigIdIntPair entry = saveManager.Current.permanentUpgrades[i];
            if (entry.value <= 0 || string.IsNullOrWhiteSpace(entry.configId))
            {
                continue;
            }

            if (!configManager.TryGetBuff(entry.configId, out BuffDataSO buff) || !buff.HasSkillBuff)
            {
                continue;
            }

            buffManager.ApplyBuff(buff, entry.value, SkillBuffApplySource.MetaPermanent);
            applied++;
        }

        return applied > 0 || saveManager.Current.permanentUpgrades.Count == 0;
    }
}
