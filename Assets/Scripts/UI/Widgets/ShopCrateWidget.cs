using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 补给箱面板：单抽 / 十连 / 广告免费抽。
/// </summary>
public class ShopCrateWidget : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private TMP_Text panelIndexText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image crateImage;
    [SerializeField] private TMP_Text rewardsHintText;

    [Header("Pull Buttons")]
    [SerializeField] private Button singlePullButton;
    [SerializeField] private TMP_Text singleCostText;
    [SerializeField] private Button tenPullButton;
    [SerializeField] private TMP_Text tenCostText;
    [SerializeField] private Button adPullButton;
    [SerializeField] private TMP_Text adPullText;

    [Header("Config")]
    [SerializeField] private string singleItemConfigId;
    [SerializeField] private string tenItemConfigId;
    [SerializeField] private string adFreeConfigId;
    [SerializeField] private string previewPoolConfigId;
    [SerializeField] private string crateTitle;

    [Header("Preview")]
    [SerializeField] private Button previewButton;

    public string SingleItemConfigId => singleItemConfigId;
    public string TenItemConfigId => tenItemConfigId;
    public string AdFreeConfigId => adFreeConfigId;
    public string PreviewPoolConfigId => previewPoolConfigId;
    public string CrateTitle => crateTitle;

    public event Action<string> PurchaseRequested;
    public event Action<string> AdFreeRequested;
    public event Action<string, string> PreviewRequested;

    private void Awake()
    {
        if (singlePullButton != null)
        {
            singlePullButton.onClick.AddListener(OnSinglePullClicked);
        }

        if (tenPullButton != null)
        {
            tenPullButton.onClick.AddListener(OnTenPullClicked);
        }

        if (adPullButton != null)
        {
            adPullButton.onClick.AddListener(OnAdPullClicked);
        }

        if (previewButton != null)
        {
            previewButton.onClick.AddListener(OnPreviewClicked);
        }
    }

    private void OnDestroy()
    {
        if (singlePullButton != null)
        {
            singlePullButton.onClick.RemoveListener(OnSinglePullClicked);
        }

        if (tenPullButton != null)
        {
            tenPullButton.onClick.RemoveListener(OnTenPullClicked);
        }

        if (adPullButton != null)
        {
            adPullButton.onClick.RemoveListener(OnAdPullClicked);
        }

        if (previewButton != null)
        {
            previewButton.onClick.RemoveListener(OnPreviewClicked);
        }
    }

    public void SetDisplay(string panelIndex, string title, string rewardsHint)
    {
        SetText(panelIndexText, panelIndex);
        SetText(titleText, title);
        SetText(rewardsHintText, rewardsHint);
    }

    public void SetCosts(string singleCost, string tenCost)
    {
        SetText(singleCostText, singleCost);
        SetText(tenCostText, tenCost);
    }

    public void SetAdPullAvailable(bool available, string label)
    {
        if (adPullButton != null)
        {
            adPullButton.interactable = available;
        }

        SetText(adPullText, label);
    }

    public void SetPullButtonsInteractable(bool singleAvailable, bool tenAvailable)
    {
        if (singlePullButton != null)
        {
            singlePullButton.interactable = singleAvailable;
        }

        if (tenPullButton != null)
        {
            tenPullButton.interactable = tenAvailable;
        }
    }

    private void OnSinglePullClicked()
    {
        if (!string.IsNullOrWhiteSpace(singleItemConfigId))
        {
            PurchaseRequested?.Invoke(singleItemConfigId);
        }
    }

    private void OnTenPullClicked()
    {
        if (!string.IsNullOrWhiteSpace(tenItemConfigId))
        {
            PurchaseRequested?.Invoke(tenItemConfigId);
        }
    }

    private void OnAdPullClicked()
    {
        if (!string.IsNullOrWhiteSpace(adFreeConfigId))
        {
            AdFreeRequested?.Invoke(adFreeConfigId);
        }
    }

    private void OnPreviewClicked()
    {
        if (!string.IsNullOrWhiteSpace(previewPoolConfigId))
        {
            PreviewRequested?.Invoke(previewPoolConfigId, crateTitle);
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
