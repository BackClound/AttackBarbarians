using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商城面板：展示货币、商品购买与 12 小时免费钻石。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。PanelId = <see cref="GameConstants.UiPanelIds.Shop"/>。</para>
/// <para><b>规则：</b>仅调用 <see cref="ShopManager"/> / 订阅资源事件，不直接改存档。</para>
/// </remarks>
public class ShopPanelUI : UiPanelBase
{
    [Serializable]
    /// <summary>商品行 Inspector 绑定：配置 ID、购买按钮与标签。</summary>
    private class ShopItemBinding
    {
        public string itemConfigId;
        public Button buyButton;
        public TMP_Text labelText;
    }

    [Header("Currency")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text diamondText;
    [SerializeField] private TMP_Text statusText;

    [Header("Actions")]
    [SerializeField] private Button freeDiamondButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private ShopItemBinding[] itemBindings;

    /// <summary>绑定免费钻石、关闭与各商品购买按钮。</summary>
    private void Awake()
    {
        if (freeDiamondButton != null)
        {
            freeDiamondButton.onClick.AddListener(OnFreeDiamondClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        if (itemBindings != null)
        {
            for (int i = 0; i < itemBindings.Length; i++)
            {
                ShopItemBinding binding = itemBindings[i];
                if (binding?.buyButton == null || string.IsNullOrWhiteSpace(binding.itemConfigId))
                {
                    continue;
                }

                string capturedId = binding.itemConfigId;
                binding.buyButton.onClick.AddListener(() => OnBuyClicked(capturedId));
            }
        }
    }

    /// <summary>订阅资源与商店购买事件。</summary>
    private void OnEnable()
    {
        GameEvents.SubscribeResourceChanged(OnResourceChanged);
        GameEvents.SubscribeShopPurchased(OnShopPurchased);
        GameEvents.SubscribeShopPurchaseFailed(OnShopPurchaseFailed);
    }

    /// <summary>取消商店事件订阅。</summary>
    private void OnDisable()
    {
        GameEvents.UnsubscribeResourceChanged(OnResourceChanged);
        GameEvents.UnsubscribeShopPurchased(OnShopPurchased);
        GameEvents.UnsubscribeShopPurchaseFailed(OnShopPurchaseFailed);
    }

    /// <summary>显示时全量刷新货币与商品状态。</summary>
    protected override void OnShow()
    {
        RefreshAll();
    }

    /// <summary>全量刷新货币、商品标签与免费钻石按钮。</summary>
    private void RefreshAll()
    {
        RefreshCurrency();
        RefreshItemLabels();
        RefreshFreeDiamondButton();
        SetStatus(string.Empty);
    }

    /// <summary>刷新金币与水晶显示。</summary>
    private void RefreshCurrency()
    {
        long gold = 0;
        long diamonds = 0;
        if (ServiceLocator.TryGet(out ResourceManager resources))
        {
            gold = resources.GetAmount(CurrencyType.Gold);
            diamonds = resources.GetAmount(CurrencyType.Diamond);
        }
        else if (ServiceLocator.TryGet(out SaveManager save) && save.Current != null)
        {
            gold = save.Current.gold;
            diamonds = save.Current.diamonds;
        }

        if (goldText != null)
        {
            goldText.text = gold.ToString();
            goldText.color = UiTechWastelandPalette.AccentAmber;
        }

        if (diamondText != null)
        {
            diamondText.text = diamonds.ToString();
            diamondText.color = UiTechWastelandPalette.PrimaryCyan;
        }
    }

    /// <summary>刷新各商品行价格、奖励与剩余次数。</summary>
    private void RefreshItemLabels()
    {
        if (itemBindings == null || !ServiceLocator.TryGet(out ShopManager shop))
        {
            return;
        }

        for (int i = 0; i < itemBindings.Length; i++)
        {
            ShopItemBinding binding = itemBindings[i];
            if (binding?.labelText == null || string.IsNullOrWhiteSpace(binding.itemConfigId))
            {
                continue;
            }

            if (shop.Catalog == null || !shop.Catalog.TryGetItem(binding.itemConfigId, out ShopItemSO item))
            {
                binding.labelText.text = binding.itemConfigId;
                continue;
            }

            int remaining = shop.GetRemainingPurchases(item);
            string limitText = item.HasPurchaseLimit ? $" 余{remaining}" : string.Empty;
            binding.labelText.text =
                $"{item.DisplayName}\n{item.PriceAmount} {FormatCurrency(item.PriceCurrency)} → {item.RewardAmount}{limitText}";
            binding.labelText.color = UiTechWastelandPalette.TextSecondary;

            if (binding.buyButton != null)
            {
                binding.buyButton.interactable = remaining > 0;
            }
        }
    }

    /// <summary>刷新 12 小时免费钻石按钮状态。</summary>
    private void RefreshFreeDiamondButton()
    {
        if (freeDiamondButton == null || !ServiceLocator.TryGet(out ShopManager shop))
        {
            return;
        }

        bool canClaim = shop.CanClaimFreeDiamond(out _, out _);
        freeDiamondButton.interactable = canClaim;

        TMP_Text label = freeDiamondButton.GetComponentInChildren<TMP_Text>();
        if (label == null)
        {
            return;
        }

        if (canClaim)
        {
            label.text = "领取免费量子钻";
            return;
        }

        if (shop.TryGetFreeDiamondCooldownRemaining(out TimeSpan remaining))
        {
            label.text = $"冷却 {Mathf.CeilToInt((float)remaining.TotalMinutes)} 分";
        }
    }

    /// <summary>商品购买按钮回调。</summary>
    private void OnBuyClicked(string itemConfigId)
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        if (!ServiceLocator.TryGet(out ShopManager shop))
        {
            SetStatus("商店未就绪");
            return;
        }

        shop.TryPurchase(itemConfigId);
    }

    /// <summary>免费领取钻石按钮回调。</summary>
    private void OnFreeDiamondClicked()
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiConfirm);
        if (!ServiceLocator.TryGet(out ShopManager shop))
        {
            SetStatus("商店未就绪");
            return;
        }

        shop.TryClaimFreeDiamond();
    }

    /// <summary>关闭商城面板。</summary>
    private void OnCloseClicked()
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        Hide();
        GameEvents.RaiseUiPanelClosed(this, GameConstants.UiPanelIds.Shop);
    }

    /// <summary>资源变化时刷新货币显示。</summary>
    private void OnResourceChanged(GameEventContext ctx) => RefreshCurrency();

    /// <summary>购买成功后刷新并提示。</summary>
    private void OnShopPurchased(GameEventContext ctx)
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiConfirm);
        RefreshAll();
        if (ctx.Payload is ShopPurchaseEventArgs args)
        {
            SetStatus($"已获得 {args.RewardAmount} {FormatReward(args.RewardType)}");
        }
    }

    /// <summary>购买失败后显示错误并刷新。</summary>
    private void OnShopPurchaseFailed(GameEventContext ctx)
    {
        if (ctx.Payload is ShopPurchaseFailedEventArgs args)
        {
            SetStatus(args.Message);
        }

        RefreshAll();
    }

    /// <summary>更新底部状态提示文案。</summary>
    private void SetStatus(string message)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = message ?? string.Empty;
        statusText.color = string.IsNullOrEmpty(message)
            ? UiTechWastelandPalette.TextSecondary
            : UiTechWastelandPalette.HazardYellow;
    }

    /// <summary>格式化货币类型为中文简称。</summary>
    private static string FormatCurrency(CurrencyType currency) =>
        currency switch
        {
            CurrencyType.Gold => "金",
            CurrencyType.Diamond => "钻",
            _ => currency.ToString(),
        };

    /// <summary>格式化奖励类型为中文名称。</summary>
    private static string FormatReward(ShopRewardType reward) =>
        reward switch
        {
            ShopRewardType.Gold => "废料金",
            ShopRewardType.Diamond => "量子钻",
            _ => reward.ToString(),
        };
}
