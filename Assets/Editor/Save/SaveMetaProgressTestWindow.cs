#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 局外 Skill / 永久 Buff 存档测试窗口：清除、编辑 skillLevels 与 permanentUpgrades，并写回磁盘或 Play 模式。
/// </summary>
public sealed class SaveMetaProgressTestWindow : EditorWindow
{
    private const string WindowTitle = "Meta Progress Test";

    private SaveData workingSave;
    private BuffMetaProgressCatalog catalog;
    private string savePath = string.Empty;
    private bool loadedFromDisk;
    private bool isDirty;
    private bool resetPlayTimeOnClear = true;
    private bool clearUpgradeCardsOnClear;

    private bool foldSkills = true;
    private bool foldGlobalBuffs = true;
    private bool foldStatBuffs = true;
    private readonly Dictionary<SkillType, bool> exclusiveFoldouts = new Dictionary<SkillType, bool>(8);

    private Vector2 scroll;

    [MenuItem("Attack Barbarians/Save/Meta Progress Test Window")]
    public static void Open()
    {
        SaveMetaProgressTestWindow window = GetWindow<SaveMetaProgressTestWindow>();
        window.titleContent = new GUIContent(WindowTitle);
        window.minSize = new Vector2(420f, 520f);
        window.Show();
    }

    private void OnEnable()
    {
        catalog ??= BuffMetaProgressCatalog.LoadDefault();
        ReloadFromDisk(silent: true);
    }

    private void OnGUI()
    {
        catalog ??= BuffMetaProgressCatalog.LoadDefault();

        EditorGUILayout.LabelField("局外 Skill / 永久 Buff 测试", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "编辑 skillLevels 与 permanentUpgrades。保存后重启战斗或点「同步到 Play」使运行时生效。",
            MessageType.Info);

        DrawToolbar();
        EditorGUILayout.Space(4f);
        DrawResetSection();
        EditorGUILayout.Space(6f);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawSkillsSection();
        EditorGUILayout.Space(6f);
        DrawGlobalBuffsSection();
        EditorGUILayout.Space(6f);
        DrawExclusiveBuffsSection();
        EditorGUILayout.Space(6f);
        DrawStatBuffsSection();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4f);
        DrawFooter();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("从磁盘加载", GUILayout.Height(24f)))
        {
            ReloadFromDisk();
        }

        EditorGUI.BeginDisabledGroup(!isDirty && loadedFromDisk);
        if (GUILayout.Button("保存到磁盘", GUILayout.Height(24f)))
        {
            SaveToDisk();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        if (GUILayout.Button("同步到 Play", GUILayout.Height(24f)))
        {
            ApplyToPlayMode();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawResetSection()
    {
        EditorGUILayout.LabelField("清除", EditorStyles.boldLabel);
        resetPlayTimeOnClear = EditorGUILayout.ToggleLeft(
            "同时清零累计游玩时长（避免时长自动解锁技能）",
            resetPlayTimeOnClear);
        clearUpgradeCardsOnClear = EditorGUILayout.ToggleLeft(
            "同时清空升级卡库存",
            clearUpgradeCardsOnClear);

        GUI.backgroundColor = new Color(1f, 0.75f, 0.75f);
        if (GUILayout.Button("清除 Skill & Buff（仅保留 Shoot Lv.1）", GUILayout.Height(28f)))
        {
            if (EditorUtility.DisplayDialog(
                    WindowTitle,
                    "将清空已解锁技能（保留射击）、永久 Buff 与局内进度。\n是否继续？",
                    "清除",
                    "取消"))
            {
                SaveMetaProgressTestUtility.ResetSkillsAndBuffs(
                    workingSave,
                    resetPlayTimeOnClear,
                    clearUpgradeCardsOnClear);
                MarkDirty();
            }
        }

        GUI.backgroundColor = Color.white;
    }

    private void DrawSkillsSection()
    {
        foldSkills = EditorGUILayout.Foldout(foldSkills, "永久技能解锁 (skillLevels)", true);
        if (!foldSkills)
        {
            return;
        }

        EditorGUI.indentLevel++;
        IReadOnlyList<SaveMetaProgressTestUtility.SkillMetaEntry> entries = SaveMetaProgressTestUtility.AllSkillEntries;
        for (int i = 0; i < entries.Count; i++)
        {
            SaveMetaProgressTestUtility.SkillMetaEntry entry = entries[i];
            EditorGUILayout.BeginHorizontal();
            bool unlocked = SaveMetaProgressTestUtility.IsSkillUnlocked(workingSave, entry.ConfigId);
            int level = SaveMetaProgressTestUtility.GetSkillLevel(workingSave, entry.ConfigId);

            EditorGUI.BeginDisabledGroup(entry.AlwaysUnlocked);
            bool newUnlocked = EditorGUILayout.Toggle(unlocked, GUILayout.Width(18f));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.LabelField(entry.Label, GUILayout.Width(160f));

            EditorGUI.BeginDisabledGroup(!entry.AlwaysUnlocked && !newUnlocked);
            int newLevel = EditorGUILayout.IntField(level, GUILayout.Width(48f));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (entry.AlwaysUnlocked)
            {
                newUnlocked = true;
                newLevel = Mathf.Max(1, newLevel);
            }

            newLevel = Mathf.Max(1, newLevel);
            if (newUnlocked != unlocked || newLevel != level)
            {
                SaveMetaProgressTestUtility.SetSkillUnlocked(workingSave, entry.ConfigId, newUnlocked, newLevel);
                MarkDirty();
            }
        }

        EditorGUI.indentLevel--;
    }

    private void DrawGlobalBuffsSection()
    {
        foldGlobalBuffs = EditorGUILayout.Foldout(foldGlobalBuffs, "通用永久 SkillBuff (Global)", true);
        if (!foldGlobalBuffs)
        {
            return;
        }

        EditorGUI.indentLevel++;
        IReadOnlyList<SkillBuffKind> kinds = SaveMetaProgressTestUtility.GetGlobalSkillBuffKinds(catalog);
        for (int i = 0; i < kinds.Count; i++)
        {
            DrawSkillBuffTierRow(kinds[i]);
        }

        EditorGUI.indentLevel--;
    }

    private void DrawExclusiveBuffsSection()
    {
        EditorGUILayout.LabelField("专属永久 SkillBuff", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        SkillType[] skillTypes =
        {
            SkillType.Shoot,
            SkillType.Lightning,
            SkillType.Thunder,
            SkillType.FireRain,
            SkillType.WaterWave,
            SkillType.Ice,
            SkillType.Heal,
        };

        for (int i = 0; i < skillTypes.Length; i++)
        {
            SkillType skillType = skillTypes[i];
            if (!exclusiveFoldouts.TryGetValue(skillType, out bool expanded))
            {
                expanded = skillType == SkillType.Shoot;
                exclusiveFoldouts[skillType] = expanded;
            }

            expanded = EditorGUILayout.Foldout(
                expanded,
                SaveMetaProgressTestUtility.GetSkillTypeLabel(skillType),
                true);
            exclusiveFoldouts[skillType] = expanded;
            if (!expanded)
            {
                continue;
            }

            EditorGUI.indentLevel++;
            IReadOnlyList<SkillBuffKind> kinds = SaveMetaProgressTestUtility.GetExclusiveKindsForSkill(catalog, skillType);
            if (kinds.Count == 0)
            {
                EditorGUILayout.LabelField("(无配置)", EditorStyles.miniLabel);
            }
            else
            {
                for (int k = 0; k < kinds.Count; k++)
                {
                    DrawSkillBuffTierRow(kinds[k]);
                }
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.indentLevel--;
    }

    private void DrawSkillBuffTierRow(SkillBuffKind kind)
    {
        int maxTier = catalog.GetMaxTier(kind);
        if (maxTier <= 0)
        {
            return;
        }

        int currentTier = catalog.GetHighestPermanentTier(workingSave, kind);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(SaveMetaProgressTestUtility.GetSkillBuffKindLabel(kind), GUILayout.MinWidth(160f));
        int newTier = EditorGUILayout.IntSlider(currentTier, 0, maxTier);
        EditorGUILayout.LabelField($"/ T{maxTier}", GUILayout.Width(40f));
        EditorGUILayout.EndHorizontal();

        if (newTier != currentTier)
        {
            catalog.SetHighestPermanentTier(workingSave, kind, newTier);
            MarkDirty();
        }
    }

    private void DrawStatBuffsSection()
    {
        foldStatBuffs = EditorGUILayout.Foldout(foldStatBuffs, "通用属性 Buff (StatBuff)", true);
        if (!foldStatBuffs)
        {
            return;
        }

        EditorGUI.indentLevel++;
        IReadOnlyList<BuffMetaProgressCatalog.StatBuffEntry> statBuffs = catalog.StatBuffs;
        if (statBuffs.Count == 0)
        {
            EditorGUILayout.LabelField("(无 StatBuff 配置)", EditorStyles.miniLabel);
        }
        else
        {
            for (int i = 0; i < statBuffs.Count; i++)
            {
                BuffMetaProgressCatalog.StatBuffEntry entry = statBuffs[i];
                int stacks = BuffMetaProgressCatalog.GetStatBuffStacks(workingSave, entry.ConfigId);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(entry.DisplayName, GUILayout.MinWidth(160f));
                int newStacks = EditorGUILayout.IntField(stacks, GUILayout.Width(60f));
                EditorGUILayout.LabelField(entry.ConfigId, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                newStacks = Mathf.Max(0, newStacks);
                if (newStacks != stacks)
                {
                    BuffMetaProgressCatalog.SetStatBuffStacks(workingSave, entry.ConfigId, newStacks);
                    MarkDirty();
                }
            }
        }

        EditorGUI.indentLevel--;
    }

    private void DrawFooter()
    {
        string dirtyLabel = isDirty ? "● 未保存" : "已同步";
        EditorGUILayout.LabelField($"存档: {savePath}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"状态: {dirtyLabel} | 自磁盘加载: {(loadedFromDisk ? "是" : "否（默认档）")}",
            EditorStyles.miniLabel);
    }

    private void ReloadFromDisk(bool silent = false)
    {
        loadedFromDisk = SaveMetaProgressTestUtility.TryLoadFromDisk(
            SaveMetaProgressTestUtility.DefaultSaveFileName,
            out SaveData data,
            out savePath);
        workingSave = data;
        isDirty = false;

        if (!silent)
        {
            Debug.Log($"[SaveMetaProgressTest] 已加载: {savePath}");
        }
    }

    private void SaveToDisk()
    {
        if (workingSave == null)
        {
            return;
        }

        if (SaveMetaProgressTestUtility.WriteToDisk(workingSave))
        {
            isDirty = false;
            loadedFromDisk = true;
            Debug.Log($"[SaveMetaProgressTest] 已保存: {savePath}");
        }
        else
        {
            Debug.LogError($"[SaveMetaProgressTest] 保存失败: {savePath}");
        }
    }

    private void ApplyToPlayMode()
    {
        if (workingSave == null)
        {
            return;
        }

        if (!SaveMetaProgressTestUtility.TryApplyToRunningSaveManager(workingSave))
        {
            EditorUtility.DisplayDialog(WindowTitle, "Play 模式下未找到 SaveManager，或导入失败。", "确定");
            return;
        }

        isDirty = false;
        Debug.Log("[SaveMetaProgressTest] 已同步到运行中的 SaveManager 并写盘。");
    }

    private void MarkDirty()
    {
        isDirty = true;
        Repaint();
    }
}
#endif
