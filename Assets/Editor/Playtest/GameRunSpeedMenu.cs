#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Play 模式下通过菜单切换运行倍速（1x~5x）。
/// </summary>
public static class GameRunSpeedMenu
{
    private const string MenuRoot = "Attack Barbarians/Playtest/Run Speed/";
    /// <summary>菜单：Play 模式下设置运行倍速 1x。</summary>

    [MenuItem(MenuRoot + "1x (Normal)", false, 100)]
    public static void SetSpeed1x() => Apply(1f);

    /// <summary>菜单：Play 模式下设置运行倍速 2x。</summary>
    [MenuItem(MenuRoot + "2x", false, 101)]
    public static void SetSpeed2x() => Apply(2f);
    /// <summary>菜单：Play 模式下设置运行倍速 3x。</summary>

    [MenuItem(MenuRoot + "3x", false, 102)]
    public static void SetSpeed3x() => Apply(3f);
    /// <summary>菜单：Play 模式下设置运行倍速 4x。</summary>

    [MenuItem(MenuRoot + "4x", false, 103)]
    public static void SetSpeed4x() => Apply(4f);

    /// <summary>菜单：Play 模式下设置运行倍速 5x。</summary>
    [MenuItem(MenuRoot + "5x (Max)", false, 104)]
    public static void SetSpeed5x() => Apply(5f);
    /// <summary>校验菜单：仅 Play 模式下启用 1x 选项。</summary>

    [MenuItem(MenuRoot + "1x (Normal)", true, 100)]
    public static bool ValidateSpeed1x() => Application.isPlaying;
    /// <summary>校验菜单：仅 Play 模式下启用 2x 选项。</summary>

    [MenuItem(MenuRoot + "2x", true, 101)]
    public static bool ValidateSpeed2x() => Application.isPlaying;

    /// <summary>校验菜单：仅 Play 模式下启用 3x 选项。</summary>
    [MenuItem(MenuRoot + "3x", true, 102)]
    public static bool ValidateSpeed3x() => Application.isPlaying;
    /// <summary>校验菜单：仅 Play 模式下启用 4x 选项。</summary>

    [MenuItem(MenuRoot + "4x", true, 103)]
    public static bool ValidateSpeed4x() => Application.isPlaying;
    /// <summary>校验菜单：仅 Play 模式下启用 5x 选项。</summary>

    [MenuItem(MenuRoot + "5x (Max)", true, 104)]
    public static bool ValidateSpeed5x() => Application.isPlaying;

    /// <summary>应用运行倍速到 GameRunSpeedSettings。</summary>
    /// <param name="multiplier">倍速系数。</param>
    private static void Apply(float multiplier)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[GameRunSpeed] 请在 Play 模式下设置运行倍速。");
            return;
        }

        GameRunSpeedSettings.SetPlaySpeedMultiplier(multiplier);
    }
}
#endif
