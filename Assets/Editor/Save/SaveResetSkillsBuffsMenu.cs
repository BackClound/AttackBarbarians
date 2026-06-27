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
        bool hadFile = SaveMetaProgressTestUtility.TryLoadFromDisk(
            SaveMetaProgressTestUtility.DefaultSaveFileName,
            out SaveData data,
            out string path);

        if (!hadFile)
        {
            data = SaveData.CreateDefault();
            path = SaveFileIO.GetSaveFilePath(SaveConstants.DefaultSaveFileName);
        }

        SaveMetaProgressTestUtility.ResetSkillsAndBuffs(data, resetPlayTime: true);
        if (SaveMetaProgressTestUtility.WriteToDisk(data, SaveMetaProgressTestUtility.DefaultSaveFileName))
        {
            Debug.Log($"[SaveReset] 已重置技能/Buff（仅 skill.shoot Lv.1，游玩时长已清零）: {path}");
        }
        else
        {
            Debug.LogError($"[SaveReset] 写入失败: {path}");
        }
    }
}
#endif
