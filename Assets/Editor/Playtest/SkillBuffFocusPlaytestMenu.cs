#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Play 模式下通过菜单开启技能 Buff 专项测试：三选一仅出现指定技能 Buff + 通用 Buff。
/// </summary>
public static class SkillBuffFocusPlaytestMenu
{
    private const string MenuRoot = "Attack Barbarians/Playtest/Skill Buff Focus/";

    [MenuItem(MenuRoot + "射击 (Shoot)", false, 100)]
    public static void FocusShoot() => Apply(SkillType.Shoot);

    [MenuItem(MenuRoot + "火雨 (FireRain)", false, 101)]
    public static void FocusFireRain() => Apply(SkillType.FireRain);

    [MenuItem(MenuRoot + "冰霜 (Ice)", false, 102)]
    public static void FocusIce() => Apply(SkillType.Ice);

    [MenuItem(MenuRoot + "闪电 (Lightning)", false, 103)]
    public static void FocusLightning() => Apply(SkillType.Lightning);

    [MenuItem(MenuRoot + "落雷 (Thunder)", false, 104)]
    public static void FocusThunder() => Apply(SkillType.Thunder);

    [MenuItem(MenuRoot + "水浪 (WaterWave)", false, 105)]
    public static void FocusWaterWave() => Apply(SkillType.WaterWave);

    [MenuItem(MenuRoot + "治疗 (Heal)", false, 106)]
    public static void FocusHeal() => Apply(SkillType.Heal);

    [MenuItem(MenuRoot + "关闭专项测试 (Normal Pool)", false, 200)]
    public static void DisableFocus() => Apply(SkillType.None);

    [MenuItem(MenuRoot + "射击 (Shoot)", true, 100)]
    public static bool ValidateFocusShoot() => ValidateMenu(SkillType.Shoot);

    [MenuItem(MenuRoot + "火雨 (FireRain)", true, 101)]
    public static bool ValidateFocusFireRain() => ValidateMenu(SkillType.FireRain);

    [MenuItem(MenuRoot + "冰霜 (Ice)", true, 102)]
    public static bool ValidateFocusIce() => ValidateMenu(SkillType.Ice);

    [MenuItem(MenuRoot + "闪电 (Lightning)", true, 103)]
    public static bool ValidateFocusLightning() => ValidateMenu(SkillType.Lightning);

    [MenuItem(MenuRoot + "落雷 (Thunder)", true, 104)]
    public static bool ValidateFocusThunder() => ValidateMenu(SkillType.Thunder);

    [MenuItem(MenuRoot + "水浪 (WaterWave)", true, 105)]
    public static bool ValidateFocusWaterWave() => ValidateMenu(SkillType.WaterWave);

    [MenuItem(MenuRoot + "治疗 (Heal)", true, 106)]
    public static bool ValidateFocusHeal() => ValidateMenu(SkillType.Heal);

    [MenuItem(MenuRoot + "关闭专项测试 (Normal Pool)", true, 200)]
    public static bool ValidateDisableFocus() => ValidateMenu(SkillType.None);

    private static void Apply(SkillType skillType)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SkillBuffFocus] 请在 Play 模式下开启技能 Buff 专项测试。");
            return;
        }

        if (skillType == SkillType.None)
        {
            SkillBuffFocusPlaytestSettings.Disable();
            return;
        }

        SkillBuffFocusPlaytestSettings.EnableFocus(skillType);
    }

    private static bool ValidateMenu(SkillType skillType)
    {
        if (!Application.isPlaying)
        {
            return false;
        }

        if (skillType == SkillType.None)
        {
            Menu.SetChecked(MenuRoot + "关闭专项测试 (Normal Pool)", !SkillBuffFocusPlaytestSettings.IsActive);
            return true;
        }

        bool isCurrent = SkillBuffFocusPlaytestSettings.IsActive &&
                         SkillBuffFocusPlaytestSettings.FocusedSkillType == skillType;
        Menu.SetChecked(MenuRoot + GetMenuLabel(skillType), isCurrent);
        return true;
    }

    private static string GetMenuLabel(SkillType skillType) => skillType switch
    {
        SkillType.Shoot => "射击 (Shoot)",
        SkillType.FireRain => "火雨 (FireRain)",
        SkillType.Ice => "冰霜 (Ice)",
        SkillType.Lightning => "闪电 (Lightning)",
        SkillType.Thunder => "落雷 (Thunder)",
        SkillType.WaterWave => "水浪 (WaterWave)",
        SkillType.Heal => "治疗 (Heal)",
        _ => skillType.ToString(),
    };
}
#endif
