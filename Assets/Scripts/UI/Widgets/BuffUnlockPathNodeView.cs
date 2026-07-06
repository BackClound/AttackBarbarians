using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单枚 Buff 解锁路径节点卡片。
/// </summary>
public class BuffUnlockPathNodeView : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image typeStripeImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button unlockButton;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject unlockedMark;

    private SkillType boundSkillType;

    /// <summary>点击待解锁节点时触发。</summary>
    public event Action<SkillType> UnlockRequested;

    /// <summary>绑定解锁按钮。</summary>
    private void Awake()
    {
        if (unlockButton != null)
        {
            unlockButton.onClick.AddListener(OnUnlockClick);
        }
    }

    /// <summary>解绑解锁按钮。</summary>
    private void OnDestroy()
    {
        if (unlockButton != null)
        {
            unlockButton.onClick.RemoveListener(OnUnlockClick);
        }
    }

    /// <summary>刷新节点展示。</summary>
    public void SetData(BuffUnlockPathNode node)
    {
        boundSkillType = node.SkillType;
        string title = BuffUnlockPresentationResolver.ResolveNodeTitle(node);
        string subtitle = BuffUnlockPresentationResolver.ResolveNodeSubtitle(node);
        Sprite icon = BuffUnlockPresentationResolver.ResolveNodeIcon(node);

        if (titleText != null)
        {
            titleText.text = title;
            titleText.color = node.State == BuffUnlockNodeState.Locked
                ? UiTechWastelandPalette.TextSecondary
                : UiTechWastelandPalette.TextPrimary;
        }

        if (subtitleText != null)
        {
            subtitleText.text = subtitle;
            subtitleText.color = node.IsGlobal
                ? UiTechWastelandPalette.PrimaryCyan
                : UiTechWastelandPalette.AccentAmber;
        }

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (typeStripeImage != null)
        {
            typeStripeImage.color = node.IsGlobal
                ? UiTechWastelandPalette.PrimaryCyan
                : UiTechWastelandPalette.AccentAmber;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = node.State switch
            {
                BuffUnlockNodeState.Pending => new Color(0.12f, 0.22f, 0.24f, 0.95f),
                BuffUnlockNodeState.Unlocked => new Color(0.1f, 0.16f, 0.14f, 0.92f),
                _ => new Color(0.1f, 0.11f, 0.13f, 0.88f),
            };
        }

        bool showCost = node.State == BuffUnlockNodeState.Pending && node.RequiredCards > 0;
        if (costText != null)
        {
            costText.gameObject.SetActive(showCost);
            costText.text = showCost ? $"需 {node.RequiredCards} 张解锁卡" : string.Empty;
            costText.color = UiTechWastelandPalette.HazardYellow;
        }

        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(node.State == BuffUnlockNodeState.Locked);
        }

        if (unlockedMark != null)
        {
            unlockedMark.SetActive(node.State == BuffUnlockNodeState.Unlocked);
        }

        if (unlockButton != null)
        {
            unlockButton.interactable = node.State == BuffUnlockNodeState.Pending;
        }
    }

    /// <summary>隐藏节点。</summary>
    public void Clear()
    {
        gameObject.SetActive(false);
    }

    private void OnUnlockClick()
    {
        UnlockRequested?.Invoke(boundSkillType);
    }
}
