#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 创建默认广告配置资产。
/// </summary>
public static class AdConfigBootstrapMenu
{
    private const string AdFolder = "Assets/Resources/Config/Ad";
    private const string ConfigPath = AdFolder + "/AdConfig_Default.asset";

    /// <summary>菜单：创建/更新默认 AdConfig 资产。</summary>
    [MenuItem("Attack Barbarians/Ad/Create Default Ad Config")]
    public static void CreateDefaultAdConfig()
    {
        Directory.CreateDirectory(AdFolder);

        AdConfigSO config = AssetDatabase.LoadAssetAtPath<AdConfigSO>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<AdConfigSO>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }

        SerializedObject so = new SerializedObject(config);
        so.FindProperty("selectionMode").enumValueIndex = (int)AdNetworkSelectionMode.AutoByPlatform;
        so.FindProperty("editorNetwork").enumValueIndex = (int)AdNetworkKind.Mock;
        so.FindProperty("androidNetwork").enumValueIndex = (int)AdNetworkKind.UnityAds;
        so.FindProperty("iosNetwork").enumValueIndex = (int)AdNetworkKind.UnityAds;
        so.FindProperty("fallbackNetwork").enumValueIndex = (int)AdNetworkKind.Mock;
        so.FindProperty("enableFallbackOnInitFailure").boolValue = true;
        so.FindProperty("enableFallbackOnLoadFailure").boolValue = true;
        so.FindProperty("maxRewardedShowAttempts").intValue = 2;
        so.FindProperty("useMockInEditor").boolValue = true;
        so.FindProperty("forceMock").boolValue = false;
        so.FindProperty("testMode").boolValue = true;
        so.FindProperty("defaultRewardedPlacementId").stringValue = "Rewarded_Android";
        so.FindProperty("defaultInterstitialPlacementId").stringValue = "Interstitial_Android";
        so.FindProperty("defaultBannerPlacementId").stringValue = "Banner_Android";
        so.FindProperty("mockAdDelaySeconds").floatValue = 0.35f;
        so.FindProperty("mockSimulateSkip").boolValue = false;
        so.FindProperty("rewardAdTicketAmount").intValue = AdTicketConstants.DefaultRewardPerAd;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = config;
        Debug.Log($"[AdConfigBootstrapMenu] 已创建/更新 {ConfigPath}");
    }
}
#endif
