using UnityEngine;

/// <summary>
/// 科技废土 UI 设计 Token（与 <c>prompt_ui_tech_wasteland_visual_design.md</c> 对齐）。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。静态色板，供 UI 组件与 <see cref="UiRarityVisual"/> 引用。</para>
/// </remarks>
public static class UiTechWastelandPalette
{
    public static readonly Color VoidBlack = Hex("#121416");
    public static readonly Color PanelSteel = Hex("#1E2228");
    public static readonly Color PanelRust = Hex("#2A241C");
    public static readonly Color Scrim = new Color(0f, 0f, 0f, 0.62f);
    public static readonly Color PrimaryCyan = Hex("#2EC4B6");
    public static readonly Color PrimaryCyanDim = Hex("#1A8A80");
    public static readonly Color AccentAmber = Hex("#FF9F1C");
    public static readonly Color DangerRed = Hex("#E63946");
    public static readonly Color TextPrimary = Hex("#E8EDF2");
    public static readonly Color TextSecondary = Hex("#8B9AAB");
    public static readonly Color TextTerminal = Hex("#65FF9A");
    public static readonly Color HpFill = Hex("#E63946");
    public static readonly Color ExpFill = Hex("#2EC4B6");
    public static readonly Color HazardYellow = Hex("#FFBE0B");
    public static readonly Color BorderHard = Hex("#3D4450");

    public static readonly Color RarityCommon = Hex("#5C6670");
    public static readonly Color RarityRare = Hex("#4CC9F0");
    public static readonly Color RarityEpic = Hex("#9B5DE5");
    public static readonly Color RarityLegendary = Hex("#F4D35E");

    /// <summary>根据升级卡稀有度返回对应边框/强调色。</summary>
        /// <param name="rarity">升级卡稀有度。</param>
        /// <returns>科技废土色板中的稀有度颜色。</returns>
    public static Color GetUpgradeRarityColor(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Uncommon:
                return RarityRare;
            case UpgradeRarity.Rare:
                return RarityRare;
            case UpgradeRarity.Epic:
                return RarityEpic;
            case UpgradeRarity.Legendary:
                return RarityLegendary;
            default:
                return RarityCommon;
        }
    }

    /// <summary>根据装备品质返回对应边框/强调色。</summary>
        /// <param name="quality">装备品质等级。</param>
        /// <returns>科技废土色板中的品质颜色。</returns>
    public static Color GetEquipmentQualityColor(EquipmentQuality quality)
    {
        switch (quality)
        {
            case EquipmentQuality.Uncommon:
                return RarityRare;
            case EquipmentQuality.Rare:
                return RarityRare;
            case EquipmentQuality.Epic:
                return RarityEpic;
            case EquipmentQuality.Legendary:
                return RarityLegendary;
            default:
                return RarityCommon;
        }
    }

    /// <summary>将十六进制色值字符串解析为 <see cref="Color"/>。</summary>
    /// <param name="hex">HTML 色值（如 #RRGGBB）。</param>
    /// <returns>解析成功返回颜色，失败返回白色。</returns>
    private static Color Hex(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }

        return Color.white;
    }
}
