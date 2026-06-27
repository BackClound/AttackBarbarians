#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 创建默认 WaveSchedule、Enemy/Boss 池并注册到 ConfigDatabase。
/// </summary>
public static class WaveScheduleBootstrapMenu
{
    private const string Folder = "Assets/Resources/Config/Wave";
    private const string SchedulePath = Folder + "/WaveSchedule_Default.asset";
    private const string EnemyPoolPath = Folder + "/WaveEnemyPool_Default.asset";
    private const string BossPoolPath = Folder + "/WaveBossPool_Default.asset";
    private const string RulesPath = Folder + "/WaveProceduralRules_Default.asset";
    private const string DatabasePath = "Assets/Resources/Config/ConfigDatabase.asset";

    [MenuItem("Attack Barbarians/Config/Create Default Wave Schedule")]
    public static void CreateDefaultWaveSchedule()
    {
        Directory.CreateDirectory(Folder);

        WaveEnemyPoolSO enemyPool = LoadOrCreate<WaveEnemyPoolSO>(EnemyPoolPath);
        WaveBossPoolSO bossPool = LoadOrCreate<WaveBossPoolSO>(BossPoolPath);
        WaveProceduralRulesSO rules = LoadOrCreate<WaveProceduralRulesSO>(RulesPath);
        WaveScheduleSO schedule = LoadOrCreate<WaveScheduleSO>(SchedulePath);

        ConfigureEnemyPool(enemyPool);
        ConfigureBossPool(bossPool);
        ConfigureProceduralRules(rules, bossPool);
        ConfigureSchedule(schedule, rules);
        RegisterInDatabase(schedule);

        EditorUtility.SetDirty(enemyPool);
        EditorUtility.SetDirty(bossPool);
        EditorUtility.SetDirty(rules);
        EditorUtility.SetDirty(schedule);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WaveScheduleBootstrap] 默认波次表已创建/更新。");
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void ConfigureEnemyPool(WaveEnemyPoolSO pool)
    {
        SerializedObject so = new SerializedObject(pool);
        SetStringList(so, "enemyConfigIds", new[]
        {
            GameConstants.ConfigIds.EnemyBat,
            "enemy.fantasy.bat",
            "enemy.fantasy.boar",
            "enemy.fantasy.wolf",
            "enemy.fantasy.slime",
            "enemy.fantasy.shroom",
            "enemy.fantasy.skeleton",
            "enemy.fantasy.golem",
            "enemy.fantasy.troll"
        });
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureBossPool(WaveBossPoolSO pool)
    {
        SerializedObject so = new SerializedObject(pool);
        SerializedProperty entries = so.FindProperty("bossEntries");
        entries.arraySize = 3;
        SetBossEntry(entries.GetArrayElementAtIndex(0), GameConstants.ConfigIds.BossBatKing, 1);
        SetBossEntry(entries.GetArrayElementAtIndex(1), "boss.dark_knight", 1);
        SetBossEntry(entries.GetArrayElementAtIndex(2), "boss.will_o_wisp", 1);
        so.FindProperty("bossSpawnAtElapsed").floatValue = 25f;
        so.FindProperty("requireBossDefeatToComplete").boolValue = true;
        so.FindProperty("pauseNormalSpawnsWhileBossAlive").boolValue = true;
        so.FindProperty("selectionMode").enumValueIndex = (int)WaveBossPoolSO.BossSelectionMode.RoundRobin;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureProceduralRules(WaveProceduralRulesSO rules, WaveBossPoolSO bossPool)
    {
        SerializedObject so = new SerializedObject(rules);
        so.FindProperty("difficultyTierWaveSize").intValue = 20;
        so.FindProperty("enemyUnlockWaveInterval").intValue = 30;
        so.FindProperty("enemyPoolSize").intValue = 3;
        so.FindProperty("bossActivePoolSize").intValue = 1;
        so.FindProperty("withinTierCombatIncrement").floatValue = 0.025f;
        so.FindProperty("maxMoveSpeedMultiplierInTier").floatValue = 1.35f;
        SetStringList(so, "baseEnemyConfigIds", new[]
        {
            GameConstants.ConfigIds.EnemyBat,
            "enemy.fantasy.bat",
            "enemy.fantasy.boar"
        });
        SetStringList(so, "enemyUnlockOrder", new[]
        {
            "enemy.fantasy.wolf",
            "enemy.fantasy.slime",
            "enemy.fantasy.shroom",
            "enemy.fantasy.skeleton",
            "enemy.fantasy.golem",
            "enemy.fantasy.troll"
        });
        SetStringList(so, "specialEnemyConfigIds", new[]
        {
            GameConstants.ConfigIds.EnemyBatCharge,
            GameConstants.ConfigIds.EnemyBatShield,
            GameConstants.ConfigIds.EnemyBatSplit,
            GameConstants.ConfigIds.EnemyBatSummoner,
            GameConstants.ConfigIds.EnemyBatRanged
        });
        so.FindProperty("bossPool").objectReferenceValue = bossPool;
        so.FindProperty("bossMilestoneInterval").intValue = 10;
        so.FindProperty("bossMilestoneBaseChance").floatValue = 0.35f;
        so.FindProperty("bossMilestoneChancePerTier").floatValue = 0.08f;
        so.FindProperty("guaranteedBossOnDifficultyTierBoundary").boolValue = true;
        so.FindProperty("maxDifficultyMultiplier").floatValue = 3.5f;
        so.FindProperty("difficultySaturationRate").floatValue = 0.45f;
        so.FindProperty("maxDifficultyTierIndex").intValue = 12;
        so.FindProperty("bossBaseStatMultiplier").floatValue = 1.2f;
        so.FindProperty("bossStatMultiplierPerTier").floatValue = 0.15f;
        so.FindProperty("bossStatMultiplierMax").floatValue = 4f;
        so.FindProperty("bossBaseCount").intValue = 1;
        so.FindProperty("bossCountPerTier").intValue = 1;
        so.FindProperty("bossCountMax").intValue = 4;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureSchedule(WaveScheduleSO schedule, WaveProceduralRulesSO rules)
    {
        SerializedObject so = new SerializedObject(schedule);
        so.FindProperty("configId").stringValue = GameConstants.ConfigIds.WaveScheduleDefault;
        so.FindProperty("displayName").stringValue = "Default Wave Schedule";
        so.FindProperty("displayTotalWaves").intValue = 0;
        so.FindProperty("useProceduralRules").boolValue = true;
        so.FindProperty("proceduralRules").objectReferenceValue = rules;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureScheduleLegacy(WaveScheduleSO schedule, WaveEnemyPoolSO enemyPool, WaveBossPoolSO bossPool)
    {
        SerializedObject so = new SerializedObject(schedule);
        so.FindProperty("useProceduralRules").boolValue = false;

        ConfigureDefaultSegment(so, enemyPool, bossPool);
        ConfigureSegments(so, enemyPool, bossPool);
        ConfigureExactOverrides(so);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureDefaultSegment(SerializedObject scheduleSo, WaveEnemyPoolSO enemyPool, WaveBossPoolSO bossPool)
    {
        SerializedProperty segment = scheduleSo.FindProperty("defaultSegment");
        segment.FindPropertyRelative("startWave").intValue = 1;
        segment.FindPropertyRelative("endWave").intValue = 0;
        segment.FindPropertyRelative("enemyPool").objectReferenceValue = enemyPool;
        segment.FindPropertyRelative("bossPool").objectReferenceValue = bossPool;
        segment.FindPropertyRelative("eliteSpawnChance").floatValue = 0.08f;
        segment.FindPropertyRelative("specialSpawnChance").floatValue = 0.12f;
        SetStringList(segment, "specialEnemyConfigIds", new[]
        {
            GameConstants.ConfigIds.EnemyBatCharge,
            GameConstants.ConfigIds.EnemyBatShield,
            GameConstants.ConfigIds.EnemyBatSplit,
            GameConstants.ConfigIds.EnemyBatSummoner,
            GameConstants.ConfigIds.EnemyBatRanged
        });
    }

    private static void ConfigureSegments(SerializedObject scheduleSo, WaveEnemyPoolSO enemyPool, WaveBossPoolSO bossPool)
    {
        SerializedProperty segments = scheduleSo.FindProperty("segments");
        segments.arraySize = 2;

        SerializedProperty early = segments.GetArrayElementAtIndex(0);
        early.FindPropertyRelative("startWave").intValue = 1;
        early.FindPropertyRelative("endWave").intValue = 10;
        early.FindPropertyRelative("enemyPool").objectReferenceValue = enemyPool;
        early.FindPropertyRelative("bossPool").objectReferenceValue = bossPool;
        early.FindPropertyRelative("eliteSpawnChance").floatValue = 0.08f;
        early.FindPropertyRelative("specialSpawnChance").floatValue = 0.12f;
        SetStringList(early, "specialEnemyConfigIds", new[]
        {
            GameConstants.ConfigIds.EnemyBatCharge,
            GameConstants.ConfigIds.EnemyBatShield,
            GameConstants.ConfigIds.EnemyBatSplit,
            GameConstants.ConfigIds.EnemyBatSummoner,
            GameConstants.ConfigIds.EnemyBatRanged
        });

        SerializedProperty mid = segments.GetArrayElementAtIndex(1);
        mid.FindPropertyRelative("startWave").intValue = 11;
        mid.FindPropertyRelative("endWave").intValue = 0;
        mid.FindPropertyRelative("enemyPool").objectReferenceValue = enemyPool;
        mid.FindPropertyRelative("bossPool").objectReferenceValue = bossPool;
        mid.FindPropertyRelative("eliteSpawnChance").floatValue = 0.12f;
        mid.FindPropertyRelative("specialSpawnChance").floatValue = 0.18f;
        SerializedProperty difficulty = mid.FindPropertyRelative("difficulty");
        difficulty.FindPropertyRelative("statMultiplier").floatValue = 1.1f;
        difficulty.FindPropertyRelative("eliteSpawnChanceAdd").floatValue = 0.02f;
        SetStringList(mid, "specialEnemyConfigIds", new[]
        {
            GameConstants.ConfigIds.EnemyBatCharge,
            GameConstants.ConfigIds.EnemyBatShield,
            GameConstants.ConfigIds.EnemyBatSplit,
            GameConstants.ConfigIds.EnemyBatSummoner,
            GameConstants.ConfigIds.EnemyBatRanged
        });
    }

    private static void ConfigureExactOverrides(SerializedObject scheduleSo)
    {
        SerializedProperty exacts = scheduleSo.FindProperty("exactOverrides");
        exacts.arraySize = 1;

        SerializedProperty wave10 = exacts.GetArrayElementAtIndex(0);
        wave10.FindPropertyRelative("waveIndex").intValue = 10;
        SerializedProperty patch = wave10.FindPropertyRelative("patch");
        patch.FindPropertyRelative("hasBoss").boolValue = true;
        patch.FindPropertyRelative("bossConfigId").stringValue = "boss.will_o_wisp";
        patch.FindPropertyRelative("bossSpawnAtElapsed").floatValue = 28f;
        patch.FindPropertyRelative("bossPool").objectReferenceValue = null;
        patch.FindPropertyRelative("eliteSpawnChance").floatValue = 0.15f;
        patch.FindPropertyRelative("specialSpawnChance").floatValue = 0.18f;
    }

    private static void RegisterInDatabase(WaveScheduleSO schedule)
    {
        ConfigDatabaseSO database = AssetDatabase.LoadAssetAtPath<ConfigDatabaseSO>(DatabasePath);
        if (database == null)
        {
            Debug.LogWarning("[WaveScheduleBootstrap] 未找到 ConfigDatabase.asset。");
            return;
        }

        SerializedObject so = new SerializedObject(database);
        SerializedProperty list = so.FindProperty("waveSchedules");
        bool exists = false;
        for (int i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == schedule)
            {
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = schedule;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    private static void SetBossEntry(SerializedProperty entry, string bossId, int weight)
    {
        entry.FindPropertyRelative("bossConfigId").stringValue = bossId;
        entry.FindPropertyRelative("weight").intValue = weight;
    }

    private static void SetStringList(SerializedObject so, string propertyName, IReadOnlyList<string> values)
    {
        SerializedProperty list = so.FindProperty(propertyName);
        SetStringList(list, propertyName, values);
    }

    private static void SetStringList(SerializedProperty parent, string propertyName, IReadOnlyList<string> values)
    {
        SerializedProperty list = parent.FindPropertyRelative(propertyName) ?? parent;
        list.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
        {
            list.GetArrayElementAtIndex(i).stringValue = values[i];
        }
    }
}
#endif
