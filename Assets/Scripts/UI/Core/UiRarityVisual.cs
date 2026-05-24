using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 按稀有度为边框/顶条着色（升级卡、装备格等）。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在卡片根或顶边 Image 上。</para>
/// </remarks>
[DisallowMultipleComponent]
public class UiRarityVisual : MonoBehaviour
{
    [SerializeField] private Image borderImage;
    [SerializeField] private Image topAccentImage;
    [SerializeField] private bool useEquipmentQuality;
    [SerializeField] private UpgradeRarity upgradeRarity = UpgradeRarity.Common;
    [SerializeField] private EquipmentQuality equipmentQuality = EquipmentQuality.Common;

    private void Reset()
    {
        borderImage = GetComponent<Image>();
    }

    public void ApplyUpgradeRarity(UpgradeRarity rarity)
    {
        useEquipmentQuality = false;
        upgradeRarity = rarity;
        Refresh();
    }

    public void ApplyEquipmentQuality(EquipmentQuality quality)
    {
        useEquipmentQuality = true;
        equipmentQuality = quality;
        Refresh();
    }

    public void Refresh()
    {
        Color color = useEquipmentQuality
            ? UiTechWastelandPalette.GetEquipmentQualityColor(equipmentQuality)
            : UiTechWastelandPalette.GetUpgradeRarityColor(upgradeRarity);

        if (borderImage != null)
        {
            borderImage.color = color;
        }

        if (topAccentImage != null)
        {
            topAccentImage.color = color;
        }
    }

    private void OnEnable()
    {
        Refresh();
    }
}
