using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商城补给箱抽取结果弹窗：单抽大卡 / 十连网格、确认、再抽、奖池预览。
/// </summary>
public class ShopCrateRewardPopupPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button scrimButton;
    [SerializeField] private TMP_Text titleCnText;
    [SerializeField] private TMP_Text titleEnText;
    [SerializeField] private GameObject singleDrawRoot;
    [SerializeField] private UpgradeCardDisplayView singleCardView;
    [SerializeField] private GameObject tenDrawRoot;
    [SerializeField] private UpgradeCardDisplayView[] tenCardSlots;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button drawAgainButton;
    [SerializeField] private TMP_Text drawAgainLabel;
    [SerializeField] private Button previewButton;
    [SerializeField] private TMP_Text tapHintText;

    private string lastPurchaseConfigId;
    private string lastPoolConfigId;
    private string lastCrateTitle;
    private Action<string, string> onPreviewRequested;
    private Action<string> onDrawAgainRequested;

    private void Awake()
    {
        if (scrimButton != null)
        {
            scrimButton.onClick.AddListener(Hide);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(Hide);
        }

        if (drawAgainButton != null)
        {
            drawAgainButton.onClick.AddListener(OnDrawAgainClicked);
        }

        if (previewButton != null)
        {
            previewButton.onClick.AddListener(OnPreviewClicked);
        }

        Hide();
    }

    private void OnDestroy()
    {
        if (scrimButton != null)
        {
            scrimButton.onClick.RemoveListener(Hide);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(Hide);
        }

        if (drawAgainButton != null)
        {
            drawAgainButton.onClick.RemoveListener(OnDrawAgainClicked);
        }

        if (previewButton != null)
        {
            previewButton.onClick.RemoveListener(OnPreviewClicked);
        }
    }

    public void BindCallbacks(Action<string, string> previewRequested, Action<string> drawAgainRequested)
    {
        onPreviewRequested = previewRequested;
        onDrawAgainRequested = drawAgainRequested;
    }

    public void Show(
        UpgradeCardGrantedEventArgs args,
        string purchaseConfigId,
        string poolConfigId,
        string crateTitle)
    {
        if (root != null)
        {
            root.SetActive(true);
        }

        lastPurchaseConfigId = purchaseConfigId;
        lastPoolConfigId = poolConfigId;
        lastCrateTitle = crateTitle;

        if (titleCnText != null)
        {
            titleCnText.text = "获得奖励";
        }

        if (titleEnText != null)
        {
            titleEnText.text = "SUPPLY ACQUIRED";
        }

        if (tapHintText != null)
        {
            tapHintText.text = "点击空白处关闭";
        }

        if (drawAgainLabel != null)
        {
            drawAgainLabel.text = "再抽一次";
        }

        int count = args.Grants?.Count ?? 0;
        bool isTenDraw = count > 1;

        if (singleDrawRoot != null)
        {
            singleDrawRoot.SetActive(!isTenDraw);
        }

        if (tenDrawRoot != null)
        {
            tenDrawRoot.SetActive(isTenDraw);
        }

        if (!isTenDraw)
        {
            ShowSingle(args);
        }
        else
        {
            ShowTen(args);
        }

        if (drawAgainButton != null)
        {
            drawAgainButton.interactable = !string.IsNullOrWhiteSpace(purchaseConfigId);
        }
    }

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    private void ShowSingle(UpgradeCardGrantedEventArgs args)
    {
        if (singleCardView == null)
        {
            return;
        }

        if (args.Grants != null && args.Grants.Count > 0)
        {
            singleCardView.SetFromGrant(args.Grants[0]);
        }
        else
        {
            singleCardView.Clear();
        }
    }

    private void ShowTen(UpgradeCardGrantedEventArgs args)
    {
        if (tenCardSlots == null)
        {
            return;
        }

        for (int i = 0; i < tenCardSlots.Length; i++)
        {
            UpgradeCardDisplayView slot = tenCardSlots[i];
            if (slot == null)
            {
                continue;
            }

            if (args.Grants != null && i < args.Grants.Count)
            {
                slot.SetFromGrant(args.Grants[i]);
            }
            else
            {
                slot.Clear();
            }
        }
    }

    private void OnDrawAgainClicked()
    {
        if (string.IsNullOrWhiteSpace(lastPurchaseConfigId))
        {
            return;
        }

        string configId = lastPurchaseConfigId;
        Hide();
        onDrawAgainRequested?.Invoke(configId);
    }

    private void OnPreviewClicked()
    {
        if (!string.IsNullOrWhiteSpace(lastPoolConfigId))
        {
            onPreviewRequested?.Invoke(lastPoolConfigId, lastCrateTitle);
        }
    }
}
