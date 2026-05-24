#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 生成默认地图与局内事件配置，并写入 ConfigDatabase。
/// </summary>
public static class MapConfigBootstrapMenu
{
    private const string MapFolder = "Assets/Resources/Config/Map";
    private const string EventFolder = "Assets/Resources/Config/Map/Events";

    [MenuItem("Attack Barbarians/Config/Create Default Map Assets")]
    public static void CreateDefaultMapAssets()
    {
        Directory.CreateDirectory(MapFolder);
        Directory.CreateDirectory(EventFolder);

        MapDataSO defaultMap = CreateOrLoadMap();
        GameplayEventDataSO swarmEvent = CreateOrLoadSwarmEvent();
        RegisterInDatabase(defaultMap, swarmEvent);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MapConfigBootstrap] 默认地图与局内事件资产已创建/更新。");
    }

    private static MapDataSO CreateOrLoadMap()
    {
        string path = $"{MapFolder}/MapData_Default.asset";
        MapDataSO map = AssetDatabase.LoadAssetAtPath<MapDataSO>(path);
        if (map == null)
        {
            map = ScriptableObject.CreateInstance<MapDataSO>();
            AssetDatabase.CreateAsset(map, path);
        }

        SerializedObject so = new SerializedObject(map);
        so.FindProperty("configId").stringValue = GameConstants.ConfigIds.MapDefault;
        so.FindProperty("displayName").stringValue = "Default Wasteland";
        so.FindProperty("bgmAudioId").stringValue = GameConstants.AudioIds.MusicMainMenu;
        so.FindProperty("recommendedDifficulty").intValue = 1;

        SerializedProperty spawnArea = so.FindProperty("spawnArea");
        spawnArea.FindPropertyRelative("viewportMin").vector2Value = new Vector2(0.1f, 0.9f);
        spawnArea.FindPropertyRelative("viewportMax").vector2Value = new Vector2(0.9f, 1.1f);
        spawnArea.FindPropertyRelative("initializeDelaySeconds").floatValue = 0.2f;

        SerializedProperty wall = so.FindProperty("wallPlacement");
        wall.FindPropertyRelative("useSceneDefault").boolValue = true;

        SerializedProperty modifiers = so.FindProperty("waveModifiers");
        modifiers.FindPropertyRelative("enemyStatMultiplier").floatValue = 1f;
        modifiers.FindPropertyRelative("spawnIntervalMultiplier").floatValue = 1f;
        modifiers.FindPropertyRelative("maxSpawnCountMultiplier").floatValue = 1f;

        SerializedProperty linkedEvents = so.FindProperty("linkedGameplayEventIds");
        linkedEvents.ClearArray();
        linkedEvents.InsertArrayElementAtIndex(0);
        linkedEvents.GetArrayElementAtIndex(0).stringValue = GameConstants.ConfigIds.GameplayEventSwarm;

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(map);
        return map;
    }

    private static GameplayEventDataSO CreateOrLoadSwarmEvent()
    {
        string path = $"{EventFolder}/GameplayEvent_Swarm.asset";
        GameplayEventDataSO eventData = AssetDatabase.LoadAssetAtPath<GameplayEventDataSO>(path);
        if (eventData == null)
        {
            eventData = ScriptableObject.CreateInstance<GameplayEventDataSO>();
            AssetDatabase.CreateAsset(eventData, path);
        }

        SerializedObject so = new SerializedObject(eventData);
        so.FindProperty("configId").stringValue = GameConstants.ConfigIds.GameplayEventSwarm;
        so.FindProperty("displayName").stringValue = "Monster Swarm";
        so.FindProperty("durationSeconds").floatValue = 12f;
        so.FindProperty("endsOnWaveComplete").boolValue = true;

        SerializedProperty trigger = so.FindProperty("trigger");
        trigger.FindPropertyRelative("triggerType").enumValueIndex = (int)GameplayEventTriggerType.OnWaveStarted;
        trigger.FindPropertyRelative("minWaveIndex").intValue = 2;
        trigger.FindPropertyRelative("maxWaveIndex").intValue = 99;
        trigger.FindPropertyRelative("triggerChance").floatValue = 0.35f;

        SerializedProperty effects = so.FindProperty("effects");
        effects.ClearArray();
        effects.InsertArrayElementAtIndex(0);
        SerializedProperty effect = effects.GetArrayElementAtIndex(0);
        effect.FindPropertyRelative("effectType").enumValueIndex = (int)GameplayEventEffectType.ModifySpawnInterval;
        effect.FindPropertyRelative("value").floatValue = 0.7f;

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(eventData);
        return eventData;
    }

    private static void RegisterInDatabase(params Object[] items)
    {
        ConfigDatabaseSO database = AssetDatabase.LoadAssetAtPath<ConfigDatabaseSO>(
            "Assets/Resources/Config/ConfigDatabase.asset");
        if (database == null)
        {
            Debug.LogWarning("[MapConfigBootstrap] 未找到 ConfigDatabase.asset。");
            return;
        }

        SerializedObject so = new SerializedObject(database);
        for (int i = 0; i < items.Length; i++)
        {
            Object item = items[i];
            if (item is MapDataSO map)
            {
                AddUnique(so.FindProperty("maps"), map);
            }
            else if (item is GameplayEventDataSO gameplayEvent)
            {
                AddUnique(so.FindProperty("gameplayEvents"), gameplayEvent);
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    private static void AddUnique(SerializedProperty list, Object item)
    {
        if (list == null || item == null)
        {
            return;
        }

        for (int i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == item)
            {
                return;
            }
        }

        int index = list.arraySize;
        list.InsertArrayElementAtIndex(index);
        list.GetArrayElementAtIndex(index).objectReferenceValue = item;
    }
}
#endif
