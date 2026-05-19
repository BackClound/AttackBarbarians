using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 存档文件读写：主档、备份、导入导出路径。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。由 <see cref="SaveManager"/> 调用。</para>
/// </remarks>
public static class SaveFileIO
{
    public static string GetSaveDirectory() => Application.persistentDataPath;

    public static string GetSaveFilePath(string fileName) =>
        Path.Combine(GetSaveDirectory(), fileName ?? SaveConstants.DefaultSaveFileName);

    public static string GetBackupFilePath(string fileName) =>
        GetSaveFilePath(fileName) + SaveConstants.BackupSuffix;

    public static string GetExportFilePath() =>
        Path.Combine(GetSaveDirectory(), SaveConstants.ExportFileName);

    public static bool TryReadText(string path, out string json)
    {
        json = null;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            json = File.ReadAllText(path);
            return !string.IsNullOrWhiteSpace(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveFileIO] 读取失败 path={path} error={ex.Message}");
            return false;
        }
    }

    public static bool TryWriteText(string path, string content, bool createBackup)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (createBackup && File.Exists(path))
            {
                string backupPath = path + SaveConstants.BackupSuffix;
                File.Copy(path, backupPath, overwrite: true);
            }

            File.WriteAllText(path, content);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveFileIO] 写入失败 path={path} error={ex.Message}");
            return false;
        }
    }

    public static bool TryDelete(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveFileIO] 删除失败 path={path} error={ex.Message}");
            return false;
        }
    }
}
