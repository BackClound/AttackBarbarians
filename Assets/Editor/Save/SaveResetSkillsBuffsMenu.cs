#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 存档调试：重置技能解锁与永久 Buff。
/// </summary>
public static class SaveResetSkillsBuffsMenu
{
    /// <summary>菜单：重置磁盘存档中的技能解锁与永久 Buff（仅保留射击 Lv.1）。</summary>
    [MenuItem("Attack Barbarians/Save/Reset Skills & Buffs (Shoot Only)")]
    public static void ResetSkillsAndBuffsOnDisk()
    {
        string path = SaveFileIO.GetSaveFilePath(SaveConstants.DefaultSaveFileName);
        if (!SaveFileIO.TryReadText(path, out string json) || string.IsNullOrWhiteSpace(json))
        {
            SaveData fresh = SaveData.CreateDefault();
            fresh.ResetUnlockedSkillsAndBuffsKeepShootOnly();
            WriteSave(path, fresh);
            Debug.Log($"[SaveReset] 无存档，已创建仅解锁射击的默认档: {path}");
            return;
        }

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveReset] 存档解析失败: {ex.Message}");
            return;
        }

        if (data == null)
        {
            Debug.LogError("[SaveReset] 存档为空，已取消。");
            return;
        }

        data = SaveVersionMigrator.Migrate(data);
        data.ResetUnlockedSkillsAndBuffsKeepShootOnly();
        WriteSave(path, data);
        Debug.Log($"[SaveReset] 已重置技能/Buff（仅 skill.shoot Lv.1）: {path}");
    }

    /// <summary>序列化 SaveData 并写入磁盘（含备份）。</summary>
    /// <param name="path">存档路径。</param>
    /// <param name="data">存档数据。</param>
    private static void WriteSave(string path, SaveData data)
    {
        data.lastSavedUtcTicks = System.DateTime.UtcNow.Ticks;
        string output = JsonUtility.ToJson(data, prettyPrint: true);
        if (!SaveFileIO.TryWriteText(path, output, createBackup: true))
        {
            Debug.LogError($"[SaveReset] 写入失败: {path}");
        }
    }
}
#endif
