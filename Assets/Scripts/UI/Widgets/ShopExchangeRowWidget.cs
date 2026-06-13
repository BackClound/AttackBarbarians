using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 广告券兑换行：票券数量、奖励预览、兑换按钮。
/// </summary>
public class ShopExchangeRowWidget : MonoBehaviour
{
    [SerializeField] private TMP_Text ticketCostText;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private Button exchangeButton;
    [SerializeField] private Image exchangeButtonImage;
    [SerializeField] private string exchangeConfigId;

    /// <summary>兑换商品配置 ID。</summary>
    public string ExchangeConfigId => exchangeConfigId;

    /// <summary>点击兑换时触发，携带配置 ID。</summary>
    public event Action<string> ExchangeRequested;

    /// <summary>绑定兑换按钮点击。</summary>
    private void Awake()
    {
        if (exchangeButton != null)
        {
            exchangeButton.onClick.AddListener(OnExchangeClicked);
        }
    }

    /// <summary>解绑兑换按钮点击。</summary>
    private void OnDestroy()
    {
        if (exchangeButton != null)
        {
            exchangeButton.onClick.RemoveListener(OnExchangeClicked);
        }
    }

    /// <summary>刷新票券消耗与奖励预览文案。</summary>
    /// <param name="ticketCost">消耗广告券数量文本。</param>
    /// <param name="rewardPreview">奖励预览文本。</param>
    public void SetDisplay(string ticketCost, string rewardPreview)
    {
        SetText(ticketCostText, ticketCost);
        SetText(rewardText, rewardPreview);
    }

    /// <summary>设置兑换按钮是否可点击。</summary>
    /// <param name="interactable">是否可交互。</param>
    public void SetInteractable(bool interactable)
    {
        if (exchangeButton != null)
        {
            exchangeButton.interactable = interactable;
        }
    }

    /// <summary>设置兑换按钮强调色。</summary>
    /// <param name="color">按钮 Image 颜色。</param>
    public void SetButtonAccent(Color color)
    {
        if (exchangeButtonImage != null)
        {
            exchangeButtonImage.color = color;
        }
    }

    /// <summary>兑换按钮回调，抛出配置 ID。</summary>
    private void OnExchangeClicked()
    {
        if (!string.IsNullOrWhiteSpace(exchangeConfigId))
        {
            ExchangeRequested?.Invoke(exchangeConfigId);
        }
    }

    /// <summary>安全写入 TMP 文本。</summary>
    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
