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

    public string ExchangeConfigId => exchangeConfigId;

    public event Action<string> ExchangeRequested;

    private void Awake()
    {
        if (exchangeButton != null)
        {
            exchangeButton.onClick.AddListener(OnExchangeClicked);
        }
    }

    private void OnDestroy()
    {
        if (exchangeButton != null)
        {
            exchangeButton.onClick.RemoveListener(OnExchangeClicked);
        }
    }

    public void SetDisplay(string ticketCost, string rewardPreview)
    {
        SetText(ticketCostText, ticketCost);
        SetText(rewardText, rewardPreview);
    }

    public void SetInteractable(bool interactable)
    {
        if (exchangeButton != null)
        {
            exchangeButton.interactable = interactable;
        }
    }

    public void SetButtonAccent(Color color)
    {
        if (exchangeButtonImage != null)
        {
            exchangeButtonImage.color = color;
        }
    }

    private void OnExchangeClicked()
    {
        if (!string.IsNullOrWhiteSpace(exchangeConfigId))
        {
            ExchangeRequested?.Invoke(exchangeConfigId);
        }
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
