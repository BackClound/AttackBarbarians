using System;
using UnityEngine;

/// <summary>
/// 存档管理器：加载/保存 JSON、自动存档、删除、导入导出，并通过 <see cref="GameEvents"/> 广播结果。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。由 <see cref="GameBootstrapper"/> 在 Config 之后初始化。</para>
/// <para><b>推荐挂载对象：</b><c>GameSystems</c> 根物体或子物体 <c>SaveManager</c>。</para>
/// <para><b>不要挂载到：</b>Player、Enemy、UI Canvas。</para>
/// <para><b>存储路径：</b><c>Application.persistentDataPath</c> 下的 JSON 文件（默认 <c>save.json</c>）。</para>
/// <para><b>获取方式：</b><c>ServiceLocator.Get&lt;SaveManager&gt;()</c>。</para>
/// <para><b>测试：</b>Inspector 右键组件 → Debug/Load、Save、Delete、Export。</para>
/// </remarks>
public class SaveManager : MonoBehaviour, IGameSystem
{
    [Header("File")]
    [SerializeField] private string saveFileName = SaveConstants.DefaultSaveFileName;
    [SerializeField] private bool createBackupOnSave = true;

    [Header("Auto Save")]
    [SerializeField] private bool enableAutoSave = true;
    [SerializeField] private float autoSaveDebounceSeconds = 2f;

    private SaveData currentData;
    private bool isDirty;
    private bool isInitialized;
    private float autoSaveCountdown;
    private bool enableLogs;

    public bool IsInitialized => isInitialized;
    public SaveData Current => currentData;
    public SettingsData Settings => currentData?.settings;
    public bool HasActiveRun => currentData?.runProgress != null && currentData.runProgress.hasActiveRun;

    public long Gold
    {
        get => currentData?.gold ?? 0;
        set
        {
            if (currentData == null)
            {
                return;
            }

            currentData.gold = Math.Max(0, value);
            MarkDirty();
        }
    }

    public long Diamonds
    {
        get => currentData?.diamonds ?? 0;
        set
        {
            if (currentData == null)
            {
                return;
            }

            currentData.diamonds = Math.Max(0, value);
            MarkDirty();
        }
    }

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        if (ServiceLocator.TryGet(out ConfigManager config) && config.GameConfig != null)
        {
            enableLogs = config.GameConfig.EnableRuntimeLogs;
            if (config.GameConfig.SaveFileName.Length > 0)
            {
                saveFileName = config.GameConfig.SaveFileName;
            }

            enableAutoSave = config.GameConfig.EnableAutoSave;
            autoSaveDebounceSeconds = config.GameConfig.AutoSaveDebounceSeconds;
        }

        Load();
        SubscribeEvents();
        isInitialized = true;
    }

    public void Tick(float deltaTime)
    {
        if (!isInitialized || !enableAutoSave || !isDirty)
        {
            return;
        }

        autoSaveCountdown -= deltaTime;
        if (autoSaveCountdown <= 0f)
        {
            Save(autoSave: true);
        }
    }

    public void Shutdown()
    {
        UnsubscribeEvents();
        if (isDirty)
        {
            Save(autoSave: false);
        }

        isInitialized = false;
    }

    /// <summary>从磁盘加载；无文件或损坏时使用默认数据并尝试备份。</summary>
    public void Load()
    {
        string path = SaveFileIO.GetSaveFilePath(saveFileName);
        SaveData loaded = TryDeserializeFromFile(path);

        if (loaded == null)
        {
            string backupPath = SaveFileIO.GetBackupFilePath(saveFileName);
            if (SaveFileIO.TryReadText(backupPath, out string backupJson))
            {
                loaded = TryDeserializeJson(backupJson);
                if (loaded != null && enableLogs)
                {
                    Debug.LogWarning("[SaveManager] 主存档不可用，已从备份恢复。");
                }
            }
        }

        currentData = SaveVersionMigrator.Migrate(loaded ?? SaveData.CreateDefault());
        isDirty = loaded == null;
        autoSaveCountdown = 0f;

        if (enableLogs)
        {
            Debug.Log($"[SaveManager] 已加载 version={currentData.version} path={path}");
        }

        GameEvents.RaiseSaveLoaded(this);
    }

    /// <summary>立即写入磁盘。</summary>
    public bool Save(bool autoSave = false)
    {
        if (currentData == null)
        {
            currentData = SaveData.CreateDefault();
        }

        currentData.version = SaveConstants.CurrentVersion;
        currentData.lastSavedUtcTicks = DateTime.UtcNow.Ticks;

        string json = JsonUtility.ToJson(currentData, prettyPrint: true);
        string path = SaveFileIO.GetSaveFilePath(saveFileName);
        bool success = SaveFileIO.TryWriteText(path, json, createBackupOnSave);

        if (!success)
        {
            Debug.LogError("[SaveManager] 保存失败，内存数据未清除 dirty 标记。");
            return false;
        }

        isDirty = false;
        autoSaveCountdown = autoSaveDebounceSeconds;

        if (enableLogs)
        {
            string mode = autoSave ? "AutoSave" : "Save";
            Debug.Log($"[SaveManager] {mode} 成功 path={path}");
        }

        GameEvents.RaiseSaveCompleted(this);
        return true;
    }

    /// <summary>标记脏数据并触发防抖自动保存。</summary>
    public void MarkDirty()
    {
        isDirty = true;
        autoSaveCountdown = autoSaveDebounceSeconds;
    }

    /// <summary>跳过防抖，立即保存。</summary>
    public void SaveImmediate() => Save(autoSave: false);

    /// <summary>删除主档与备份，并重建默认存档。</summary>
    public void DeleteSave()
    {
        SaveFileIO.TryDelete(SaveFileIO.GetSaveFilePath(saveFileName));
        SaveFileIO.TryDelete(SaveFileIO.GetBackupFilePath(saveFileName));
        currentData = SaveData.CreateDefault();
        isDirty = true;
        SaveImmediate();

        if (enableLogs)
        {
            Debug.Log("[SaveManager] 存档已删除并重建默认数据。");
        }
    }

    /// <summary>导出当前存档 JSON 到 persistentDataPath/save_export.json。</summary>
    public string ExportToFile()
    {
        if (currentData == null)
        {
            return null;
        }

        string json = JsonUtility.ToJson(currentData, prettyPrint: true);
        string exportPath = SaveFileIO.GetExportFilePath();
        if (!SaveFileIO.TryWriteText(exportPath, json, createBackup: false))
        {
            return null;
        }

        if (enableLogs)
        {
            Debug.Log($"[SaveManager] 已导出 path={exportPath}");
        }

        return exportPath;
    }

    /// <summary>返回当前存档 JSON 字符串，便于调试复制。</summary>
    public string ExportToJson() =>
        currentData != null ? JsonUtility.ToJson(currentData, prettyPrint: true) : string.Empty;

    /// <summary>从 JSON 字符串导入并覆盖当前内存存档（不自动写盘，需调用 Save）。</summary>
    public bool ImportFromJson(string json)
    {
        SaveData imported = TryDeserializeJson(json);
        if (imported == null)
        {
            return false;
        }

        currentData = SaveVersionMigrator.Migrate(imported);
        MarkDirty();
        return true;
    }

    /// <summary>从导出文件或任意路径导入 JSON。</summary>
    public bool ImportFromFile(string path)
    {
        if (!SaveFileIO.TryReadText(path, out string json))
        {
            return false;
        }

        return ImportFromJson(json);
    }

    public void BeginRun(int startingWave = 1, int startingLevel = 1, float hp = 0f, float maxHp = 0f)
    {
        if (currentData?.runProgress == null)
        {
            return;
        }

        RunProgressData run = currentData.runProgress;
        run.hasActiveRun = true;
        run.currentWave = Mathf.Max(1, startingWave);
        run.currentLevel = Mathf.Max(1, startingLevel);
        run.currentHp = hp;
        run.maxHp = maxHp;
        run.activeBuffs?.Clear();
        currentData.statistics.totalRuns++;
        MarkDirty();
    }

    public void ClearActiveRun()
    {
        currentData?.runProgress?.ClearRun();
        MarkDirty();
    }

    public void UpdateRunProgress(int wave, int level, float hp, float maxHp)
    {
        if (currentData?.runProgress == null || !currentData.runProgress.hasActiveRun)
        {
            return;
        }

        RunProgressData run = currentData.runProgress;
        run.currentWave = wave;
        run.currentLevel = level;
        run.currentHp = hp;
        run.maxHp = maxHp;

        if (currentData.statistics != null && wave > currentData.statistics.highestWave)
        {
            currentData.statistics.highestWave = wave;
        }

        MarkDirty();
    }

    [ContextMenu("Debug/Load")]
    private void DebugLoad() => Load();

    [ContextMenu("Debug/Save")]
    private void DebugSave() => SaveImmediate();

    [ContextMenu("Debug/Delete")]
    private void DebugDelete() => DeleteSave();

    [ContextMenu("Debug/Export")]
    private void DebugExport() => ExportToFile();

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && isDirty)
        {
            Save(autoSave: false);
        }
    }

    private void OnApplicationQuit()
    {
        if (isDirty)
        {
            Save(autoSave: false);
        }
    }

    private void SubscribeEvents()
    {
        GameEvents.SubscribeGameStateChanged(OnGameStateChanged);
        GameEvents.SubscribeWaveCompleted(OnWaveCompleted);
        GameEvents.SubscribeUpgradeSelectionCompleted(OnUpgradeSelectionCompleted);
        GameEvents.SubscribeGameOver(OnGameOver);
    }

    private void UnsubscribeEvents()
    {
        GameEvents.UnsubscribeGameStateChanged(OnGameStateChanged);
        GameEvents.UnsubscribeWaveCompleted(OnWaveCompleted);
        GameEvents.UnsubscribeUpgradeSelectionCompleted(OnUpgradeSelectionCompleted);
        GameEvents.UnsubscribeGameOver(OnGameOver);
    }

    private void OnGameStateChanged(GameEventContext ctx)
    {
        if (ctx.Payload is not GameStateChange change)
        {
            return;
        }

        if (change.NewState == GameState.Exiting)
        {
            SaveImmediate();
        }
    }

    private void OnWaveCompleted(GameEventContext ctx)
    {
        if (ctx.Payload is WaveEventArgs args && currentData?.runProgress != null)
        {
            currentData.runProgress.currentWave = args.WaveIndex;
            if (currentData.statistics != null && args.WaveIndex > currentData.statistics.highestWave)
            {
                currentData.statistics.highestWave = args.WaveIndex;
            }
        }

        RequestAutoSave();
    }

    private void OnUpgradeSelectionCompleted(GameEventContext ctx) => RequestAutoSave();

    private void OnGameOver(GameEventContext ctx)
    {
        ClearActiveRun();
        SaveImmediate();
    }

    private void RequestAutoSave()
    {
        MarkDirty();
        if (!enableAutoSave)
        {
            SaveImmediate();
        }
    }

    private SaveData TryDeserializeFromFile(string path)
    {
        if (!SaveFileIO.TryReadText(path, out string json))
        {
            return null;
        }

        return TryDeserializeJson(json);
    }

    private static SaveData TryDeserializeJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] JSON 反序列化失败: {ex.Message}");
            return null;
        }
    }
}
