#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 创建默认性能预算资产并关联到 GameConfig。
/// </summary>
public static class PerformanceConfigBootstrapMenu
{
    private const string Folder = "Assets/Resources/Config/Performance";
    private const string BudgetPath = Folder + "/PerformanceBudget_Default.asset";
    private const string GameConfigPath = "Assets/Resources/Config/GameConfig.asset";

    [MenuItem("Attack Barbarians/Performance/Create Default Performance Assets")]
    public static void CreateDefaultPerformanceAssets()
    {
        Directory.CreateDirectory(Folder);

        PerformanceBudgetSO budget = AssetDatabase.LoadAssetAtPath<PerformanceBudgetSO>(BudgetPath);
        if (budget == null)
        {
            budget = ScriptableObject.CreateInstance<PerformanceBudgetSO>();
            AssetDatabase.CreateAsset(budget, BudgetPath);
        }

        GameConfig gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
        if (gameConfig != null)
        {
            SerializedObject so = new SerializedObject(gameConfig);
            so.FindProperty("performanceBudget").objectReferenceValue = budget;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gameConfig);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = budget;
        Debug.Log("[Performance] 已创建 PerformanceBudget_Default 并写入 GameConfig.performanceBudget。");
    }
}
#endif
