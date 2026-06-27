#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 扫描全部 EnemyData Prefab 的染色能力，输出审计报告。
/// </summary>
public static class EnemyVisualTintAuditMenu
{
    private const string DatabasePath = "Assets/Resources/Config/ConfigDatabase.asset";

    [MenuItem("Attack Barbarians/Config/Audit Enemy Visual Tint Capability")]
    public static void AuditAllEnemyPrefabs()
    {
        ConfigDatabaseSO database = AssetDatabase.LoadAssetAtPath<ConfigDatabaseSO>(DatabasePath);
        if (database == null || database.Enemies == null)
        {
            Debug.LogError("[EnemyVisualTintAudit] 未找到 ConfigDatabase 或 Enemies 列表。");
            return;
        }

        StringBuilder report = new StringBuilder(2048);
        report.AppendLine("=== Enemy Prefab 染色能力审计 ===");
        int supported = 0;
        int unsupported = 0;

        for (int i = 0; i < database.Enemies.Count; i++)
        {
            EnemyDataSO enemy = database.Enemies[i];
            if (enemy == null)
            {
                continue;
            }

            EnemyVisualTintUtility.TintAuditResult result =
                EnemyVisualTintUtility.AuditPrefab(enemy.ConfigId, enemy.Prefab);

            if (result.SupportsDifficultyTint)
            {
                supported++;
            }
            else
            {
                unsupported++;
            }

            report.AppendLine(
                $"[{result.ConfigId}] {result.PrefabName} | {result.Capability} | " +
                $"SR={result.SpriteRendererCount} Spine={result.SpineSkeletonCount} Mesh={result.MeshRendererCount} | " +
                $"Tint={(result.SupportsDifficultyTint ? "YES" : "NO")}");
        }

        report.AppendLine($"--- 合计：支持染色 {supported}，不支持 {unsupported} ---");
        report.AppendLine("结论：SpriteRenderer / Spine Skeleton / 带 _Color 的 Mesh 均可通过 WaveSpawnDifficultyContext 档位染色。");
        Debug.Log(report.ToString());
    }
}
#endif
