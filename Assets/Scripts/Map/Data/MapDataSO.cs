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

    public Sprite BackgroundSprite => backgroundSprite;
    public GameObject BackgroundPrefab => backgroundPrefab;
    public string BgmAudioId => bgmAudioId;
    public MapSpawnAreaConfig SpawnArea => spawnArea;
    public MapWallPlacementConfig WallPlacement => wallPlacement;
    public MapCameraBoundsConfig CameraBounds => cameraBounds;
    public int RecommendedDifficulty => Mathf.Max(1, recommendedDifficulty);
    public MapWaveModifierConfig WaveModifiers => waveModifiers;
    public IReadOnlyList<string> PreferredEnemyConfigIds => preferredEnemyConfigIds;
    public IReadOnlyList<string> LinkedGameplayEventIds => linkedGameplayEventIds;
    public string AddressableBackgroundKey => addressableBackgroundKey;

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
