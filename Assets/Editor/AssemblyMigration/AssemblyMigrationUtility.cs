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

    public static AssemblyMigrationPhase GetAppliedPhase()
    {
        return (AssemblyMigrationPhase)EditorPrefs.GetInt(PhasePrefsKey, (int)AssemblyMigrationPhase.None);
    }

    public static void SetAppliedPhase(AssemblyMigrationPhase phase)
    {
        EditorPrefs.SetInt(PhasePrefsKey, (int)phase);
    }

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

    private static string GetAsmrefPath(string folder) => $"{folder}/{AsmrefFileName}";
}
#endif
