#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 程序集分阶段迁移菜单（阶段 6 / architecture_design §6.1）。
/// </summary>
public static class AssemblyMigrationMenu
{
    /// <summary>菜单：显示程序集迁移状态报告。</summary>
    [MenuItem("Attack Barbarians/Assembly/Status Report")]
    public static void ShowStatus()
    {
        string report = AssemblyMigrationUtility.BuildStatusReport();
        Debug.Log(report);
        EditorUtility.DisplayDialog("Assembly Migration", report, "OK");
    }

    /// <summary>菜单：应用阶段 1（Core Singleton）。</summary>
    [MenuItem("Attack Barbarians/Assembly/Apply Phase 1 (Core Singleton)")]
    public static void ApplyPhase1()
    {
        ApplyPhase(AssemblyMigrationPhase.CoreSingleton);
    }

    /// <summary>菜单：应用阶段 2（Core Foundation）。</summary>
    [MenuItem("Attack Barbarians/Assembly/Apply Phase 2 (Core Foundation)")]
    public static void ApplyPhase2()
    {
        ApplyPhase(AssemblyMigrationPhase.CoreFoundation);
    }

    /// <summary>菜单：应用阶段 3（Config）。</summary>
    [MenuItem("Attack Barbarians/Assembly/Apply Phase 3 (Config)")]
    public static void ApplyPhase3()
    {
        ApplyPhase(AssemblyMigrationPhase.Config);
    }

    /// <summary>菜单：应用阶段 4（Combat）。</summary>
    [MenuItem("Attack Barbarians/Assembly/Apply Phase 4 (Combat)")]
    public static void ApplyPhase4() => ApplyPhase(AssemblyMigrationPhase.Combat);

    /// <summary>菜单：应用阶段 5（Skills）。</summary>
    [MenuItem("Attack Barbarians/Assembly/Apply Phase 5 (Skills)")]
    public static void ApplyPhase5() => ApplyPhase(AssemblyMigrationPhase.Skills);

    /// <summary>菜单：应用阶段 6（Gameplay）。</summary>
    [MenuItem("Attack Barbarians/Assembly/Apply Phase 6 (Gameplay)")]
    public static void ApplyPhase6() => ApplyPhase(AssemblyMigrationPhase.Gameplay);

    /// <summary>菜单：应用阶段 7（Meta）。</summary>
    [MenuItem("Attack Barbarians/Assembly/Apply Phase 7 (Meta)")]
    public static void ApplyPhase7() => ApplyPhase(AssemblyMigrationPhase.Meta);

    /// <summary>菜单：应用阶段 8（Presentation）。</summary>
    [MenuItem("Attack Barbarians/Assembly/Apply Phase 8 (Presentation)")]
    public static void ApplyPhase8() => ApplyPhase(AssemblyMigrationPhase.Presentation);

    /// <summary>菜单：应用阶段 9（App）。</summary>
    [MenuItem("Attack Barbarians/Assembly/Apply Phase 9 (App)")]
    public static void ApplyPhase9() => ApplyPhase(AssemblyMigrationPhase.App);

    /// <summary>菜单：弹窗选择目标阶段并应用迁移。</summary>
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

    /// <summary>菜单：应用完整迁移至 Editor 阶段（含全部运行时绑定）。</summary>
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

    /// <summary>菜单：移除全部 asmref，恢复默认 Assembly-CSharp 编译域。</summary>
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

    /// <summary>应用指定迁移阶段并弹出结果对话框。</summary>
    /// <param name="phase">目标迁移阶段。</param>
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
