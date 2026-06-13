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
    /// <summary>获取存档根目录（<c>Application.persistentDataPath</c>）。</summary>
    /// <returns>本地持久化数据目录的绝对路径。</returns>
    public static string GetSaveDirectory() => Application.persistentDataPath;

    /// <summary>获取主存档文件的完整路径。</summary>
    /// <param name="fileName">存档文件名；为 null 时使用默认文件名。</param>
    /// <returns>主存档 JSON 文件的绝对路径。</returns>
    public static string GetSaveFilePath(string fileName) =>
        Path.Combine(GetSaveDirectory(), fileName ?? SaveConstants.DefaultSaveFileName);

    /// <summary>获取备份存档文件的完整路径。</summary>
    /// <param name="fileName">主存档文件名。</param>
    /// <returns>备份文件（主档 + .bak）的绝对路径。</returns>
    public static string GetBackupFilePath(string fileName) =>
        GetSaveFilePath(fileName) + SaveConstants.BackupSuffix;

    /// <summary>获取导出存档文件的完整路径。</summary>
    /// <returns>导出 JSON 文件（save_export.json）的绝对路径。</returns>
    public static string GetExportFilePath() =>
        Path.Combine(GetSaveDirectory(), SaveConstants.ExportFileName);

    /// <summary>
    /// 尝试从磁盘读取文本文件内容。
    /// </summary>
    /// <param name="path">文件绝对路径。</param>
    /// <param name="json">读取到的文本内容；失败时为 null。</param>
    /// <returns>读取成功且内容非空时返回 true，否则返回 false。</returns>
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

    /// <summary>
    /// 尝试将文本内容写入磁盘，可选在写入前创建备份。
    /// </summary>
    /// <param name="path">目标文件绝对路径。</param>
    /// <param name="content">要写入的文本内容。</param>
    /// <param name="createBackup">写入前是否将现有文件复制为 .bak 备份。</param>
    /// <returns>写入成功时返回 true，否则返回 false。</returns>
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

    /// <summary>
    /// 尝试删除指定路径的文件。
    /// </summary>
    /// <param name="path">目标文件绝对路径。</param>
    /// <returns>删除成功时返回 true；文件不存在或失败时返回 false。</returns>
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
