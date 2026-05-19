#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Config Database 检视器：提供一键校验按钮。
/// </summary>
[CustomEditor(typeof(ConfigDatabaseSO))]
public class ConfigDatabaseSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Validate Config Database"))
        {
            var database = (ConfigDatabaseSO)target;
            ConfigValidationResult result = ConfigValidator.ValidateDatabase(database);
            if (result.IsValid)
            {
                Debug.Log($"[{database.name}] 配置校验通过。");
            }
            else
            {
                Debug.LogError($"[{database.name}] 配置校验失败:\n{result.BuildReport()}");
            }
        }
    }
}
#endif
