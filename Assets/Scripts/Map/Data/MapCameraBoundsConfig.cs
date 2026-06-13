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

    /// <summary>正交相机尺寸覆盖值（≤0 时不覆盖）。</summary>
    public float OrthographicSizeOverride => orthographicSizeOverride;
    /// <summary>相机背景色。</summary>
    public Color BackgroundColor => backgroundColor;
    /// <summary>是否覆盖相机背景色。</summary>
    public bool OverrideBackgroundColor => overrideBackgroundColor;
}
