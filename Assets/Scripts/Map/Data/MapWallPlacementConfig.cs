using UnityEngine;

/// <summary>
/// 地图墙体位置：留空则保持场景中已有墙体 Transform。
/// </summary>
[System.Serializable]
public struct MapWallPlacementConfig
{
    [SerializeField] private bool useSceneDefault;
    [SerializeField] private Vector3 worldPosition;

    public bool UseSceneDefault => useSceneDefault;
    public Vector3 WorldPosition => worldPosition;
}
