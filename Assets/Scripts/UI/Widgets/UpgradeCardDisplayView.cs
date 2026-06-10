using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 升级卡展示组件：英文标题、图标、中文名、描述、稀有度星与边框。
/// </summary>
public class UpgradeCardDisplayView : MonoBehaviour
{
    [SerializeField] private Image borderImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text englishTitleText;
    [SerializeField] private TMP_Text chineseTitleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text dropRateText;
    [SerializeField] private UiRarityVisual rarityVisual;
    [SerializeField] private Image[] starImages;

    public void SetData(UpgradeCardSO card, string resolvedConfigId = null, float? dropRatePercent = null)
    {
        if (card == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        string configId = string.IsNullOrWhiteSpace(resolvedConfigId) ? card.ConfigId : resolvedConfigId;

        if (englishTitleText != null)
        {
            englishTitleText.text = UpgradeCardDisplayNames.GetEnglishTitle(configId);
        }

        if (chineseTitleText != null)
        {
            chineseTitleText.text = ResolveChineseTitle(card, configId);
        }

        if (descriptionText != null)
        {
            descriptionText.text = ResolveDescription(card, configId);
        }

        Sprite icon = card.Icon;
        if (icon == null && ServiceLocator.TryGet(out ConfigManager config))
        {
            icon = TryResolveSkillIcon(config, configId);
        }

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (rarityVisual != null)
        {
            rarityVisual.ApplyUpgradeRarity(card.Rarity);
        }
        else if (borderImage != null)
        {
            borderImage.color = UiTechWastelandPalette.GetUpgradeRarityColor(card.Rarity);
        }

        ApplyStars(UpgradeCardDisplayNames.GetStarCount(card.Rarity));
        SetDropRate(dropRatePercent);
    }

    public void SetPreviewEntry(UpgradeCardPoolPreviewEntry entry)
    {
        if (entry.Card != null)
        {
            SetData(entry.Card, entry.CardConfigId, entry.DropRatePercent);
            return;
        }

        gameObject.SetActive(true);
        if (englishTitleText != null)
        {
            englishTitleText.text = UpgradeCardDisplayNames.GetEnglishTitle(entry.CardConfigId);
        }

        if (chineseTitleText != null)
        {
            chineseTitleText.text = entry.CardConfigId;
        }

        SetDropRate(entry.DropRatePercent);
    }

    public void SetFromGrant(UpgradeCardGrantEntry grant)
    {
        if (ServiceLocator.TryGet(out UpgradeCardManager manager) &&
            manager.TryResolveCard(grant.CardConfigId, out UpgradeCardSO card))
        {
            SetData(card, grant.CardConfigId);
            return;
        }

        gameObject.SetActive(true);
        if (englishTitleText != null)
        {
            englishTitleText.text = UpgradeCardDisplayNames.GetEnglishTitle(grant.CardConfigId);
        }

        if (chineseTitleText != null)
        {
            chineseTitleText.text = grant.DisplayName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = string.Empty;
        }

        SetDropRate(null);
    }

    public void Clear()
    {
        gameObject.SetActive(false);
    }

    private static string ResolveChineseTitle(UpgradeCardSO card, string configId)
    {
        if (card.Category == UpgradeCardCategory.GenericSkill ||
            card.Category == UpgradeCardCategory.GenericAttribute)
        {
            if (configId != card.ConfigId &&
                ServiceLocator.TryGet(out UpgradeCardManager manager) &&
                manager.TryResolveCard(configId, out UpgradeCardSO resolved))
            {
                return resolved.DisplayName;
            }
        }

        return card.DisplayName;
    }

    private static string ResolveDescription(UpgradeCardSO card, string configId)
    {
        if (card.Category == UpgradeCardCategory.GenericSkill ||
            card.Category == UpgradeCardCategory.GenericAttribute)
        {
            if (configId != card.ConfigId &&
                ServiceLocator.TryGet(out UpgradeCardManager manager) &&
                manager.TryResolveCard(configId, out UpgradeCardSO resolved) &&
                !string.IsNullOrWhiteSpace(resolved.Description))
            {
                return resolved.Description;
            }
        }

        return card.Description;
    }

    private static Sprite TryResolveSkillIcon(ConfigManager config, string cardConfigId)
    {
        string skillId = cardConfigId switch
        {
            UpgradeCardConstants.CardIds.SkillShoot => GameConstants.ConfigIds.SkillShoot,
            UpgradeCardConstants.CardIds.SkillFireRain => GameConstants.ConfigIds.SkillFireRain,
            UpgradeCardConstants.CardIds.SkillIce => GameConstants.ConfigIds.SkillIce,
            UpgradeCardConstants.CardIds.SkillLightning => GameConstants.ConfigIds.SkillLightning,
            UpgradeCardConstants.CardIds.SkillThunder => GameConstants.ConfigIds.SkillThunder,
            UpgradeCardConstants.CardIds.SkillWaterWave => GameConstants.ConfigIds.SkillWaterWave,
            UpgradeCardConstants.CardIds.SkillHeal => GameConstants.ConfigIds.SkillHeal,
            _ => null,
        };

        if (string.IsNullOrWhiteSpace(skillId))
        {
            return null;
        }

        return config.TryGetSkill(skillId, out SkillDataSO skill) ? skill.Icon : null;
    }

    private void SetDropRate(float? dropRatePercent)
    {
        if (dropRateText == null)
        {
            return;
        }

        if (dropRatePercent.HasValue)
        {
            dropRateText.gameObject.SetActive(true);
            dropRateText.text = $"爆率 {dropRatePercent.Value:0.0}%";
        }
        else
        {
            dropRateText.gameObject.SetActive(false);
            dropRateText.text = string.Empty;
        }
    }

    private void ApplyStars(int count)
    {
        if (starImages == null)
        {
            return;
        }

        int clamped = Mathf.Clamp(count, 0, starImages.Length);
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] != null)
            {
                starImages[i].enabled = i < clamped;
            }
        }
    }
}
