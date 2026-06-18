#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 创建默认 WaveProgression 资产并注册到 ConfigDatabase / GameConfig。
/// </summary>
public static class WaveProgressionConfigBootstrapMenu
{
    private const string Folder = "Assets/Resources/Config/Wave";
    private const string AssetPath = Folder + "/WaveProgression_Default.asset";
    private const string DatabasePath = "Assets/Resources/Config/ConfigDatabase.asset";
    private const string GameConfigPath = "Assets/Resources/Config/GameConfig.asset";

    [MenuItem("Attack Barbarians/Config/Create Default Wave Progression")]
    public static void CreateDefaultWaveProgression()
    {
        Directory.CreateDirectory(Folder);

        WaveProgressionConfigSO progression = AssetDatabase.LoadAssetAtPath<WaveProgressionConfigSO>(AssetPath);
        if (progression == null)
        {
            progression = ScriptableObject.CreateInstance<WaveProgressionConfigSO>();
            AssetDatabase.CreateAsset(progression, AssetPath);
        }

        RegisterInDatabase(progression);
        RegisterInGameConfig(progression);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WaveProgressionBootstrap] 默认波次成长资产已创建/更新。");
    }

    private static void RegisterInDatabase(WaveProgressionConfigSO progression)
    {
        ConfigDatabaseSO database = AssetDatabase.LoadAssetAtPath<ConfigDatabaseSO>(DatabasePath);
        if (database == null)
        {
            Debug.LogWarning("[WaveProgressionBootstrap] 未找到 ConfigDatabase.asset。");
            return;
        }

        SerializedObject so = new SerializedObject(database);
        so.FindProperty("waveProgression").objectReferenceValue = progression;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    private static void RegisterInGameConfig(WaveProgressionConfigSO progression)
    {
        GameConfig gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
        if (gameConfig == null)
        {
            Debug.LogWarning("[WaveProgressionBootstrap] 未找到 GameConfig.asset。");
            return;
        }

        SerializedObject so = new SerializedObject(gameConfig);
        so.FindProperty("useWaveProgressionV2").boolValue = true;
        so.FindProperty("waveProgressionConfig").objectReferenceValue = progression;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gameConfig);
    }
}
#endif
