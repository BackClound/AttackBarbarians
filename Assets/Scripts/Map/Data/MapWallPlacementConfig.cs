using UnityEngine;

/// <summary>
/// 地图墙体位置：留空则保持场景中已有墙体 Transform。
/// </summary>
[System.Serializable]
public struct MapWallPlacementConfig
{
    [SerializeField] private bool useSceneDefault;
    [SerializeField] private Vector3 worldPosition;

    /// <summary>是否使用场景中已有的墙体位置。</summary>
    public bool UseSceneDefault => useSceneDefault;
    /// <summary>覆盖用的墙体世界坐标。</summary>
    public Vector3 WorldPosition => worldPosition;
}
