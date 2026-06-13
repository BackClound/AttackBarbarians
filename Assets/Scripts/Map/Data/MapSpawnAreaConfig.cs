using UnityEngine;

/// <summary>
/// 地图敌人生成区域：基于相机视口的矩形范围（与 <see cref="SpawnAreaController"/> 算法一致）。
/// </summary>
[System.Serializable]
public struct MapSpawnAreaConfig
{
    [SerializeField] private Vector2 viewportMin;
    [SerializeField] private Vector2 viewportMax;
    [SerializeField] private float initializeDelaySeconds;

    /// <summary>视口矩形左下角（归一化坐标）。</summary>
    public Vector2 ViewportMin => viewportMin;
    /// <summary>视口矩形右上角（归一化坐标）。</summary>
    public Vector2 ViewportMax => viewportMax;
    /// <summary>初始化边界前的延迟（秒）。</summary>
    public float InitializeDelaySeconds => Mathf.Max(0f, initializeDelaySeconds);

    /// <summary>项目默认生成区域配置。</summary>
    public static MapSpawnAreaConfig Default =>
        new MapSpawnAreaConfig
        {
            viewportMin = new Vector2(0.1f, 0.9f),
            viewportMax = new Vector2(0.9f, 1.1f),
            initializeDelaySeconds = 0.2f
        };
}
