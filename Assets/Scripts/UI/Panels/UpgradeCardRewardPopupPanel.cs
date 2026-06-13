using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 升级卡奖励弹窗：展示本次随机获得的升级卡列表。
/// </summary>
public class UpgradeCardRewardPopupPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text cardListText;
    [SerializeField] private Button confirmButton;

    /// <summary>绑定确认按钮，默认隐藏弹窗。</summary>
    private void Awake()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(Hide);
        }

        Hide();
    }

    /// <summary>订阅升级卡发放事件。</summary>
    private void OnEnable()
    {
        GameEvents.SubscribeUpgradeCardGranted(OnUpgradeCardGranted);
    }

    /// <summary>取消订阅升级卡发放事件。</summary>
    private void OnDisable()
    {
        GameEvents.UnsubscribeUpgradeCardGranted(OnUpgradeCardGranted);
    }

    /// <summary>解绑确认按钮。</summary>
    private void OnDestroy()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(Hide);
        }
    }

    /// <summary>根据发放来源展示升级卡列表。</summary>
    /// <param name="args">升级卡发放事件参数。</param>
    public void Show(UpgradeCardGrantedEventArgs args)
    {
        if (root != null)
        {
            root.SetActive(true);
        }

        if (titleText != null)
        {
            titleText.text = FormatSourceTitle(args.Source);
        }

        if (cardListText != null)
        {
            cardListText.text = BuildCardListText(args.Grants);
        }
    }

    /// <summary>关闭升级卡奖励弹窗。</summary>
    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    /// <summary>升级卡发放事件回调，自动弹出展示。</summary>
    private void OnUpgradeCardGranted(GameEventContext ctx)
    {
        if (ctx.Payload is UpgradeCardGrantedEventArgs args)
        {
            Show(args);
        }
    }

    /// <summary>按奖励来源格式化弹窗标题。</summary>
    private static string FormatSourceTitle(UpgradeCardRewardSource source) =>
        source switch
        {
            UpgradeCardRewardSource.ShopCrate => "补给箱奖励",
            UpgradeCardRewardSource.OfflineReward => "离线收益",
            UpgradeCardRewardSource.OnlineReward => "在线奖励",
            UpgradeCardRewardSource.Lottery => "幸运抽奖",
            UpgradeCardRewardSource.DailyReward => "七日签到",
            UpgradeCardRewardSource.RunSettlement => "战斗结算",
            UpgradeCardRewardSource.StageReward => "通关奖励",
            _ => "升级卡奖励",
        };

    /// <summary>将发放列表格式化为多行文本。</summary>
    private static string BuildCardListText(System.Collections.Generic.IReadOnlyList<UpgradeCardGrantEntry> grants)
    {
        if (grants == null || grants.Count == 0)
        {
            return "未获得升级卡";
        }

        var builder = new StringBuilder(grants.Count * 24);
        for (int i = 0; i < grants.Count; i++)
        {
            UpgradeCardGrantEntry entry = grants[i];
            builder.Append("• ");
            builder.Append(entry.DisplayName);
            if (entry.Count > 1)
            {
                builder.Append(" x");
                builder.Append(entry.Count);
            }

            if (i < grants.Count - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }
}
