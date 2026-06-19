#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

/// <summary>
/// 生成 / 移除 <c>.asmref</c>、搬移资源，并报告程序集迁移状态。
/// </summary>
public static class AssemblyMigrationUtility
{
    private const string AsmrefFileName = "AttackBarbarians.asmref";
    private const string PhasePrefsKey = "AttackBarbarians.AssemblyMigrationPhase";

    /// <summary>读取 EditorPrefs 中已应用的迁移阶段。</summary>
    public static AssemblyMigrationPhase GetAppliedPhase()
    {
        return (AssemblyMigrationPhase)EditorPrefs.GetInt(PhasePrefsKey, (int)AssemblyMigrationPhase.None);
    }

    /// <summary>写入已应用的迁移阶段到 EditorPrefs。</summary>
    /// <param name="phase">目标阶段。</param>
    public static void SetAppliedPhase(AssemblyMigrationPhase phase)
    {
        EditorPrefs.SetInt(PhasePrefsKey, (int)phase);
    }

    /// <summary>按序搬移资源、应用 asmref 并推进至目标阶段。</summary>
    /// <param name="targetPhase">目标阶段。</param>
    /// <param name="report">操作日志输出。</param>
    public static bool ApplyThroughPhase(AssemblyMigrationPhase targetPhase, out string report)
    {
        var log = new StringBuilder(512);
        AssemblyMigrationPhase current = GetAppliedPhase();

        if (targetPhase < current)
        {
            report = $"目标阶段 {targetPhase} 低于已应用阶段 {current}。请先使用 Reset 或 Apply 更高阶段。";
            return false;
        }

        if (!RunAssetMoves(targetPhase, log))
        {
            report = log.ToString();
            return false;
        }

        var bindingsToApply = new List<AssemblyMigrationCatalog.FolderBinding>();
        foreach (AssemblyMigrationCatalog.FolderBinding binding in AssemblyMigrationCatalog.GetBindingsUpTo(targetPhase))
        {
            bindingsToApply.Add(binding);
        }

        for (int i = 0; i < bindingsToApply.Count; i++)
        {
            if (!TryApplyAsmref(bindingsToApply[i], log))
            {
                report = log.ToString();
                return false;
            }
        }

        RemoveStaleAsmrefs(bindingsToApply, log);
        SetAppliedPhase(targetPhase);
        AssetDatabase.Refresh();
        CompilationPipeline.RequestScriptCompilation();

        log.AppendLine($"[Assembly] 已应用至阶段 {targetPhase} ({(int)targetPhase})。");
        report = log.ToString();
        return true;
    }

    /// <summary>移除全部 asmref 并重置阶段记录。</summary>
    /// <param name="report">操作日志输出。</param>
    public static bool ResetAll(out string report)
    {
        var log = new StringBuilder(256);
        int removed = 0;

        foreach (AssemblyMigrationCatalog.FolderBinding binding in AssemblyMigrationCatalog.FolderBindings)
        {
            string path = GetAsmrefPath(binding.Folder);
            if (!File.Exists(path))
            {
                continue;
            }

            if (AssetDatabase.DeleteAsset(path))
            {
                removed++;
            }
        }

        SetAppliedPhase(AssemblyMigrationPhase.None);
        AssetDatabase.Refresh();
        CompilationPipeline.RequestScriptCompilation();
        log.AppendLine($"[Assembly] 已移除 {removed} 个 asmref，恢复默认 Assembly-CSharp 编译域。");
        report = log.ToString();
        return true;
    }

    /// <summary>生成各目录 asmref 与 asmdef 状态报告文本。</summary>
    public static string BuildStatusReport()
    {
        var log = new StringBuilder(1024);
        AssemblyMigrationPhase applied = GetAppliedPhase();
        log.AppendLine($"已应用阶段: {applied} ({(int)applied})");
        log.AppendLine();

        foreach (AssemblyMigrationCatalog.FolderBinding binding in AssemblyMigrationCatalog.FolderBindings)
        {
            bool hasRef = File.Exists(GetAsmrefPath(binding.Folder));
            bool active = binding.MinimumPhase <= applied;
            string state = hasRef ? "asmref" : "—";
            log.AppendLine($"[{binding.AssemblyName}] {binding.Folder}  phase>={binding.MinimumPhase}  {state}  {(active ? "✓" : "")}");
        }

        log.AppendLine();
        log.AppendLine("程序集定义:");
        foreach (string assemblyName in new[]
                 {
                     "AB.Core", "AB.Config", "AB.Combat", "AB.Skills", "AB.Gameplay", "AB.Meta", "AB.Presentation",
                     "AB.App", "AB.Editor"
                 })
        {
            string asmdefPath = AssemblyMigrationCatalog.GetAsmdefPath(assemblyName);
            bool exists = File.Exists(asmdefPath);
            log.AppendLine($"  {assemblyName}: {(exists ? asmdefPath : "缺失")}");
        }

        return log.ToString();
    }

    /// <summary>执行目标阶段所需的资源搬移。</summary>
    /// <param name="targetPhase">目标阶段。</param>
    /// <param name="log">日志构建器。</param>
    private static bool RunAssetMoves(AssemblyMigrationPhase targetPhase, StringBuilder log)
    {
        foreach (AssemblyMigrationCatalog.AssetMove move in AssemblyMigrationCatalog.GetMovesUpTo(targetPhase))
        {
            if (File.Exists(move.Destination))
            {
                continue;
            }

            if (!File.Exists(move.Source))
            {
                log.AppendLine($"[Assembly] 跳过搬移（源不存在）: {move.Source}");
                continue;
            }

            string destDir = Path.GetDirectoryName(move.Destination);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            string error = AssetDatabase.MoveAsset(move.Source, move.Destination);
            if (!string.IsNullOrEmpty(error))
            {
                log.AppendLine($"[Assembly] 搬移失败: {move.Source} → {move.Destination}: {error}");
                return false;
            }

            log.AppendLine($"[Assembly] 已搬移: {move.Source} → {move.Destination}");
        }

        return true;
    }

    /// <summary>在指定目录写入 asmref 绑定。</summary>
    /// <param name="binding">文件夹绑定信息。</param>
    /// <param name="log">日志构建器。</param>
    private static bool TryApplyAsmref(AssemblyMigrationCatalog.FolderBinding binding, StringBuilder log)
    {
        if (!Directory.Exists(binding.Folder))
        {
            log.AppendLine($"[Assembly] 跳过（目录不存在）: {binding.Folder}");
            return true;
        }

        string asmdefPath = AssemblyMigrationCatalog.GetAsmdefPath(binding.AssemblyName);
        if (!File.Exists(asmdefPath))
        {
            log.AppendLine($"[Assembly] 缺少 asmdef: {asmdefPath}");
            return false;
        }

        string guid = AssetDatabase.AssetPathToGUID(asmdefPath);
        if (string.IsNullOrEmpty(guid))
        {
            log.AppendLine($"[Assembly] 无法解析 GUID: {asmdefPath}");
            return false;
        }

        string asmrefPath = GetAsmrefPath(binding.Folder);
        string json = $"{{\n    \"reference\": \"{guid}\"\n}}";
        File.WriteAllText(asmrefPath, json);

        if (asmrefPath.EndsWith(".asmref"))
        {
            // Unity 需要 .meta；Refresh 会生成
        }

        log.AppendLine($"[Assembly] asmref → {binding.AssemblyName}: {binding.Folder}");
        return true;
    }

    /// <summary>删除不在当前激活列表中的过期 asmref。</summary>
    /// <param name="activeBindings">当前生效的绑定列表。</param>
    /// <param name="log">日志构建器。</param>
    private static void RemoveStaleAsmrefs(
        IReadOnlyList<AssemblyMigrationCatalog.FolderBinding> activeBindings,
        StringBuilder log)
    {
        var activeFolders = new HashSet<string>();
        for (int i = 0; i < activeBindings.Count; i++)
        {
            activeFolders.Add(activeBindings[i].Folder);
        }

        foreach (AssemblyMigrationCatalog.FolderBinding binding in AssemblyMigrationCatalog.FolderBindings)
        {
            if (activeFolders.Contains(binding.Folder))
            {
                continue;
            }

            string path = GetAsmrefPath(binding.Folder);
            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
                log.AppendLine($"[Assembly] 移除过期 asmref: {binding.Folder}");
            }
        }
    }

    /// <summary>返回目录下 asmref 文件路径。</summary>
    /// <param name="folder">目标文件夹。</param>
    private static string GetAsmrefPath(string folder) => $"{folder}/{AsmrefFileName}";
}
#endif
