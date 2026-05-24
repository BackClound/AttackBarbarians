#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 生成默认音频配置条目并写入 AudioDatabase（Clip 需在 Inspector 中手动指定）。
/// </summary>
public static class AudioConfigBootstrapMenu
{
    private const string AudioFolder = "Assets/Resources/Config/Audio";
    private const string DatabasePath = AudioFolder + "/AudioDatabase.asset";

    private static readonly (string id, AudioChannel channel, bool loop)[] DefaultEntries =
    {
        (GameConstants.AudioIds.MusicMainMenu, AudioChannel.Bgm, true),
        (GameConstants.AudioIds.MusicPaused, AudioChannel.Bgm, true),
        (GameConstants.AudioIds.MusicGameOver, AudioChannel.Bgm, true),
        (GameConstants.AudioIds.MusicUpgrade, AudioChannel.Bgm, true),
        (GameConstants.AudioIds.MusicBoss, AudioChannel.Boss, true),
        (GameConstants.AudioIds.MusicGameplay, AudioChannel.Bgm, true),
        (GameConstants.AudioIds.SfxUiClick, AudioChannel.Ui, false),
        (GameConstants.AudioIds.SfxUiConfirm, AudioChannel.Ui, false),
        (GameConstants.AudioIds.SfxBossSpawn, AudioChannel.Sfx, false),
        (GameConstants.AudioIds.SfxBossPhase, AudioChannel.Sfx, false),
        (GameConstants.AudioIds.SfxBossDefeat, AudioChannel.Sfx, false),
        (GameConstants.AudioIds.SfxSpecialEnemySpawn, AudioChannel.Sfx, false),
        (GameConstants.AudioIds.SfxSpecialEnemyAbility, AudioChannel.Sfx, false),
        (GameConstants.AudioIds.SfxPlayerHurt, AudioChannel.Sfx, false),
        (GameConstants.AudioIds.SfxEnemyHit, AudioChannel.Sfx, false),
        (GameConstants.AudioIds.SfxEnemyKill, AudioChannel.Sfx, false),
        (GameConstants.AudioIds.SfxSkillCast, AudioChannel.Sfx, false),
    };

    [MenuItem("Attack Barbarians/Audio/Create Default Audio Assets")]
    public static void CreateDefaultAudioAssets()
    {
        Directory.CreateDirectory(AudioFolder);

        var configs = new List<AudioConfigSO>(DefaultEntries.Length);
        for (int i = 0; i < DefaultEntries.Length; i++)
        {
            (string id, AudioChannel channel, bool loop) entry = DefaultEntries[i];
            configs.Add(CreateOrUpdateConfig(entry.id, entry.channel, entry.loop));
        }

        AudioDatabaseSO database = CreateOrLoadDatabase();
        SerializedObject dbSo = new SerializedObject(database);
        SerializedProperty entriesProp = dbSo.FindProperty("entries");
        entriesProp.ClearArray();
        for (int i = 0; i < configs.Count; i++)
        {
            entriesProp.InsertArrayElementAtIndex(i);
            entriesProp.GetArrayElementAtIndex(i).objectReferenceValue = configs[i];
        }

        dbSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AudioConfigBootstrap] 默认音频配置已创建/更新。请在各 AudioConfig 上分配 AudioClip。");
    }

    private static AudioConfigSO CreateOrUpdateConfig(string audioId, AudioChannel channel, bool loop)
    {
        string safeName = audioId.Replace('.', '_');
        string path = $"{AudioFolder}/{safeName}.asset";

        AudioConfigSO config = AssetDatabase.LoadAssetAtPath<AudioConfigSO>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<AudioConfigSO>();
            AssetDatabase.CreateAsset(config, path);
        }

        SerializedObject so = new SerializedObject(config);
        so.FindProperty("configId").stringValue = audioId;
        so.FindProperty("displayName").stringValue = audioId;
        so.FindProperty("channel").enumValueIndex = (int)channel;
        so.FindProperty("loop").boolValue = loop;
        so.FindProperty("volume").floatValue = 1f;
        so.FindProperty("pitch").floatValue = 1f;

        if (channel == AudioChannel.Sfx || channel == AudioChannel.Ui)
        {
            so.FindProperty("minIntervalSeconds").floatValue =
                audioId == GameConstants.AudioIds.SfxEnemyHit ? 0.06f : 0.03f;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
        return config;
    }

    private static AudioDatabaseSO CreateOrLoadDatabase()
    {
        AudioDatabaseSO database = AssetDatabase.LoadAssetAtPath<AudioDatabaseSO>(DatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<AudioDatabaseSO>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        return database;
    }
}
#endif
