using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 广告券兑换中心：持有数量与水晶/金币兑换行。
/// </summary>
public class ShopAdTicketExchangePanel : MonoBehaviour
{
    [SerializeField] private TMP_Text sectionTitleText;
    [SerializeField] private TMP_Text heldTicketText;
    [SerializeField] private ShopExchangeRowWidget[] diamondRows;
    [SerializeField] private ShopExchangeRowWidget[] goldRows;

    /// <summary>兑换行请求兑换时向上层抛出配置 ID。</summary>
    public event Action<string> ExchangeRequested;

    /// <summary>绑定兑换行事件。</summary>
    private void Awake()
    {
        WireRows(diamondRows);
        WireRows(goldRows);
    }

    /// <summary>解绑兑换行事件。</summary>
    private void OnDestroy()
    {
        UnwireRows(diamondRows);
        UnwireRows(goldRows);
    }

    /// <summary>刷新持有广告券数量与各兑换行状态。</summary>
    public void Refresh()
    {
        if (sectionTitleText != null)
        {
            sectionTitleText.text = "广告券兑换中心";
        }

        int held = 0;
        if (ServiceLocator.TryGet(out ShopManager shop))
        {
            held = shop.GetAdTicketCount();
        }

        if (heldTicketText != null)
        {
            heldTicketText.text = $"持有数量 {held}";
        }

        RefreshRows(diamondRows);
        RefreshRows(goldRows);
    }

    /// <summary>刷新一组兑换行的价格与可用性。</summary>
    private void RefreshRows(ShopExchangeRowWidget[] rows)
    {
        if (rows == null || !ServiceLocator.TryGet(out ShopManager shop))
        {
            return;
        }

        for (int i = 0; i < rows.Length; i++)
        {
            ShopExchangeRowWidget row = rows[i];
            if (row == null || string.IsNullOrWhiteSpace(row.ExchangeConfigId))
            {
                continue;
            }

            bool canExchange = shop.CanExchangeAdTickets(row.ExchangeConfigId, out _, out _);
            row.SetInteractable(canExchange);
            shop.TryGetExchangeDisplay(row.ExchangeConfigId, out string ticketCost, out string rewardPreview);
            row.SetDisplay(ticketCost, rewardPreview);
        }
    }

    /// <summary>绑定兑换行点击事件。</summary>
    private void WireRows(ShopExchangeRowWidget[] rows)
    {
        if (rows == null)
        {
            return;
        }

        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] != null)
            {
                rows[i].ExchangeRequested += OnExchangeRequested;
            }
        }
    }

    /// <summary>解绑兑换行点击事件。</summary>
    private void UnwireRows(ShopExchangeRowWidget[] rows)
    {
        if (rows == null)
        {
            return;
        }

        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] != null)
            {
                rows[i].ExchangeRequested -= OnExchangeRequested;
            }
        }
    }

    /// <summary>转发兑换行请求到上层。</summary>
    private void OnExchangeRequested(string configId)
    {
        ExchangeRequested?.Invoke(configId);
    }
}
