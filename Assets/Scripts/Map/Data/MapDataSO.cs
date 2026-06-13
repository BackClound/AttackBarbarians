using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地图配置：背景、出生区域、墙体位置、推荐难度、BGM 与波次修正。
/// </summary>
[CreateAssetMenu(fileName = "MapData", menuName = "Attack Barbarians/Config/Map Data")]
public class MapDataSO : ConfigDataBase
{
    [Header("Presentation")]
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private GameObject backgroundPrefab;
    [SerializeField] private string bgmAudioId;

    [Header("Layout")]
    [SerializeField] private MapSpawnAreaConfig spawnArea = MapSpawnAreaConfig.Default;
    [SerializeField] private MapWallPlacementConfig wallPlacement;
    [SerializeField] private MapCameraBoundsConfig cameraBounds;

    [Header("Difficulty")]
    [SerializeField] private int recommendedDifficulty = 1;
    [SerializeField] private MapWaveModifierConfig waveModifiers = MapWaveModifierConfig.Identity;

    [Header("Content (optional)")]
    [Tooltip("留空则使用波次表默认敌人组合。")]
    [SerializeField] private List<string> preferredEnemyConfigIds = new List<string>();
    [SerializeField] private List<string> linkedGameplayEventIds = new List<string>();

    [Header("Addressables (optional)")]
    [SerializeField] private string addressableBackgroundKey;

    /// <summary>背景 Sprite（可选）。</summary>
    public Sprite BackgroundSprite => backgroundSprite;
    /// <summary>背景 Prefab（可选）。</summary>
    public GameObject BackgroundPrefab => backgroundPrefab;
    /// <summary>BGM 音频 Id。</summary>
    public string BgmAudioId => bgmAudioId;
    /// <summary>敌人生成区域配置。</summary>
    public MapSpawnAreaConfig SpawnArea => spawnArea;
    /// <summary>墙体位置配置。</summary>
    public MapWallPlacementConfig WallPlacement => wallPlacement;
    /// <summary>相机边界配置。</summary>
    public MapCameraBoundsConfig CameraBounds => cameraBounds;
    /// <summary>推荐难度等级。</summary>
    public int RecommendedDifficulty => Mathf.Max(1, recommendedDifficulty);
    /// <summary>波次全局修正配置。</summary>
    public MapWaveModifierConfig WaveModifiers => waveModifiers;
    /// <summary>优先使用的敌人 configId 列表。</summary>
    public IReadOnlyList<string> PreferredEnemyConfigIds => preferredEnemyConfigIds;
    /// <summary>地图加载时自动触发的局内事件 Id 列表。</summary>
    public IReadOnlyList<string> LinkedGameplayEventIds => linkedGameplayEventIds;
    /// <summary>Addressables 背景资源 Key（可选）。</summary>
    public string AddressableBackgroundKey => addressableBackgroundKey;

    /// <summary>
    /// 收集配置校验错误与警告。
    /// </summary>
    /// <param name="result">校验结果收集器。</param>
    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (backgroundSprite == null && backgroundPrefab == null &&
            string.IsNullOrWhiteSpace(addressableBackgroundKey))
        {
            result.AddWarning(name, "未配置背景 Sprite、Prefab 或 Addressables Key。");
        }

        if (spawnArea.ViewportMax.x <= spawnArea.ViewportMin.x ||
            spawnArea.ViewportMax.y <= spawnArea.ViewportMin.y)
        {
            result.AddError(name, "spawnArea 视口范围无效。");
        }
    }
}
