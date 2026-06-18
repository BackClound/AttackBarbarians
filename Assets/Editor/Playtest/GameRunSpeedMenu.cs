#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Play 模式下通过菜单切换运行倍速（1x~5x）。
/// </summary>
public static class GameRunSpeedMenu
{
    private const string MenuRoot = "Attack Barbarians/Playtest/Run Speed/";

    [MenuItem(MenuRoot + "1x (Normal)", false, 100)]
    public static void SetSpeed1x() => Apply(1f);

    [MenuItem(MenuRoot + "2x", false, 101)]
    public static void SetSpeed2x() => Apply(2f);

    [MenuItem(MenuRoot + "3x", false, 102)]
    public static void SetSpeed3x() => Apply(3f);

    [MenuItem(MenuRoot + "4x", false, 103)]
    public static void SetSpeed4x() => Apply(4f);

    [MenuItem(MenuRoot + "5x (Max)", false, 104)]
    public static void SetSpeed5x() => Apply(5f);

    [MenuItem(MenuRoot + "1x (Normal)", true, 100)]
    public static bool ValidateSpeed1x() => Application.isPlaying;

    [MenuItem(MenuRoot + "2x", true, 101)]
    public static bool ValidateSpeed2x() => Application.isPlaying;

    [MenuItem(MenuRoot + "3x", true, 102)]
    public static bool ValidateSpeed3x() => Application.isPlaying;

    [MenuItem(MenuRoot + "4x", true, 103)]
    public static bool ValidateSpeed4x() => Application.isPlaying;

    [MenuItem(MenuRoot + "5x (Max)", true, 104)]
    public static bool ValidateSpeed5x() => Application.isPlaying;

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
