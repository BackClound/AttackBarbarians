using UnityEngine;

/// <summary>
/// 地图相机边界与视口修正。
/// </summary>
[System.Serializable]
public struct MapCameraBoundsConfig
{
    [Tooltip("大于 0 时覆盖主相机正交尺寸。")]
    [SerializeField] private float orthographicSizeOverride;
    [SerializeField] private Color backgroundColor;
    [SerializeField] private bool overrideBackgroundColor;

    public float OrthographicSizeOverride => orthographicSizeOverride;
    public Color BackgroundColor => backgroundColor;
    public bool OverrideBackgroundColor => overrideBackgroundColor;
}
