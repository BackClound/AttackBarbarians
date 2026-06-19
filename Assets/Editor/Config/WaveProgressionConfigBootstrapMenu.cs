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

    /// <summary>菜单：创建默认 WaveProgression 配置。</summary>
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

    /// <summary>将 WaveProgression 注册到 ConfigDatabase。</summary>
    /// <param name="progression">波次成长配置。</param>
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

    /// <summary>启用 GameConfig 的 WaveProgression V2 并绑定引用。</summary>
    /// <param name="progression">波次成长配置。</param>
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
