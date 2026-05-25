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

    private void OnEnable()
    {
        GameEvents.SubscribeResourceChanged(OnResourceChanged);
        GameEvents.SubscribeShopPurchased(OnShopPurchased);
        GameEvents.SubscribeShopPurchaseFailed(OnShopPurchaseFailed);
    }

    private void OnDisable()
    {
        GameEvents.UnsubscribeResourceChanged(OnResourceChanged);
        GameEvents.UnsubscribeShopPurchased(OnShopPurchased);
        GameEvents.UnsubscribeShopPurchaseFailed(OnShopPurchaseFailed);
    }

    protected override void OnShow()
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshCurrency();
        RefreshItemLabels();
        RefreshFreeDiamondButton();
        SetStatus(string.Empty);
    }

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

    private void OnCloseClicked()
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        Hide();
        GameEvents.RaiseUiPanelClosed(this, GameConstants.UiPanelIds.Shop);
    }

    private void OnResourceChanged(GameEventContext ctx) => RefreshCurrency();

    private void OnShopPurchased(GameEventContext ctx)
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiConfirm);
        RefreshAll();
        if (ctx.Payload is ShopPurchaseEventArgs args)
        {
            SetStatus($"已获得 {args.RewardAmount} {FormatReward(args.RewardType)}");
        }
    }

    private void OnShopPurchaseFailed(GameEventContext ctx)
    {
        if (ctx.Payload is ShopPurchaseFailedEventArgs args)
        {
            SetStatus(args.Message);
        }

        RefreshAll();
    }

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

    private static string FormatCurrency(CurrencyType currency) =>
        currency switch
        {
            CurrencyType.Gold => "金",
            CurrencyType.Diamond => "钻",
            _ => currency.ToString(),
        };

    private static string FormatReward(ShopRewardType reward) =>
        reward switch
        {
            ShopRewardType.Gold => "废料金",
            ShopRewardType.Diamond => "量子钻",
            _ => reward.ToString(),
        };
}
