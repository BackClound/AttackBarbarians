#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 程序集分阶段迁移菜单（阶段 6 / architecture_design §6.1）。
/// </summary>
public static class AssemblyMigrationMenu
{
    [MenuItem("Attack Barbarians/Assembly/Status Report")]
    public static void ShowStatus()
    {
        string report = AssemblyMigrationUtility.BuildStatusReport();
        Debug.Log(report);
        EditorUtility.DisplayDialog("Assembly Migration", report, "OK");
    }

    [MenuItem("Attack Barbarians/Assembly/Apply Phase 1 (Core Singleton)")]
    public static void ApplyPhase1()
    {
        ApplyPhase(AssemblyMigrationPhase.CoreSingleton);
    }

    [MenuItem("Attack Barbarians/Assembly/Apply Phase 2 (Core Foundation)")]
    public static void ApplyPhase2()
    {
        ApplyPhase(AssemblyMigrationPhase.CoreFoundation);
    }

    [MenuItem("Attack Barbarians/Assembly/Apply Phase 3 (Config)")]
    public static void ApplyPhase3()
    {
        ApplyPhase(AssemblyMigrationPhase.Config);
    }

    [MenuItem("Attack Barbarians/Assembly/Apply Phase 4 (Combat)")]
    public static void ApplyPhase4() => ApplyPhase(AssemblyMigrationPhase.Combat);

    [MenuItem("Attack Barbarians/Assembly/Apply Phase 5 (Skills)")]
    public static void ApplyPhase5() => ApplyPhase(AssemblyMigrationPhase.Skills);

    [MenuItem("Attack Barbarians/Assembly/Apply Phase 6 (Gameplay)")]
    public static void ApplyPhase6() => ApplyPhase(AssemblyMigrationPhase.Gameplay);

    [MenuItem("Attack Barbarians/Assembly/Apply Phase 7 (Meta)")]
    public static void ApplyPhase7() => ApplyPhase(AssemblyMigrationPhase.Meta);

    [MenuItem("Attack Barbarians/Assembly/Apply Phase 8 (Presentation)")]
    public static void ApplyPhase8() => ApplyPhase(AssemblyMigrationPhase.Presentation);

    [MenuItem("Attack Barbarians/Assembly/Apply Phase 9 (App)")]
    public static void ApplyPhase9() => ApplyPhase(AssemblyMigrationPhase.App);

    [MenuItem("Attack Barbarians/Assembly/Apply Through Phase...")]
    public static void ApplyThroughPhaseDialog()
    {
        int current = (int)AssemblyMigrationUtility.GetAppliedPhase();
        int selected = EditorUtility.DisplayDialogComplex(
            "Apply Through Phase",
            $"当前已应用阶段: {(AssemblyMigrationPhase)current}\n选择要推进到的目标阶段（含该阶段所有绑定）。",
            "Phase 1",
            "Cancel",
            "Phase 4");

        if (selected == 1)
        {
            return;
        }

        AssemblyMigrationPhase target = selected == 0
            ? AssemblyMigrationPhase.CoreSingleton
            : AssemblyMigrationPhase.Combat;

        if (EditorUtility.DisplayDialog(
                "Confirm",
                $"将应用至 {target}。建议每阶段后在 Console 确认编译无错误。",
                "Apply",
                "Cancel"))
        {
            ApplyPhase(target);
        }
    }

    [MenuItem("Attack Barbarians/Assembly/Apply Complete Migration (Phase 10 Editor)")]
    public static void ApplyComplete()
    {
        if (!EditorUtility.DisplayDialog(
                "Complete Migration",
                "将应用至 Editor 阶段（含 Config、Combat、Meta 等全部运行时绑定 + Editor/Config）。\n" +
                "首次执行前请备份；若编译失败请使用 Reset。",
                "Apply",
                "Cancel"))
        {
            return;
        }

        ApplyPhase(AssemblyMigrationPhase.Editor);
    }

    [MenuItem("Attack Barbarians/Assembly/Reset All Asmrefs")]
    public static void ResetAll()
    {
        if (!EditorUtility.DisplayDialog(
                "Reset Assembly Migration",
                "移除所有 asmref，脚本回到 Assembly-CSharp。不会撤销资源搬移。",
                "Reset",
                "Cancel"))
        {
            return;
        }

        if (!AssemblyMigrationUtility.ResetAll(out string report))
        {
            Debug.LogWarning(report);
            return;
        }

        Debug.Log(report);
    }

    private static void ApplyPhase(AssemblyMigrationPhase phase)
    {
        if (!AssemblyMigrationUtility.ApplyThroughPhase(phase, out string report))
        {
            Debug.LogError(report);
            EditorUtility.DisplayDialog("Assembly Migration Failed", report, "OK");
            return;
        }

        Debug.Log(report);
        EditorUtility.DisplayDialog("Assembly Migration", report, "OK");
    }
}
#endif
