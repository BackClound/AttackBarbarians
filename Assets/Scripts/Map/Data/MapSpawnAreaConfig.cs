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

    public Vector2 ViewportMin => viewportMin;
    public Vector2 ViewportMax => viewportMax;
    public float InitializeDelaySeconds => Mathf.Max(0f, initializeDelaySeconds);

    public static MapSpawnAreaConfig Default =>
        new MapSpawnAreaConfig
        {
            viewportMin = new Vector2(0.1f, 0.9f),
            viewportMax = new Vector2(0.9f, 1.1f),
            initializeDelaySeconds = 0.2f
        };
}
