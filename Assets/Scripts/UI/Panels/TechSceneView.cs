using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 科技页 Presenter：Buff 解锁路径 + 解锁卡背包。
/// </summary>
/// <remarks>
/// <para><b>层级：</b><c>TechPageRoot</c> → <c>PageBackground</c> + <c>SafeAreaRoot</c></para>
/// <para>上 60% 路径列表可纵滑；下 40% 卡包可纵滑；各行内路径不可横滑。</para>
/// </remarks>
public class TechSceneView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private Image backgroundImage;

    [Header("Sub Panels")]
    [SerializeField] private TechBuffUnlockPathPanel pathPanel;
    [SerializeField] private TechBuffUnlockCardBagPanel cardBagPanel;
    [SerializeField] private TMP_Text statusText;

    private bool isSubscribed;

    /// <summary>订阅事件并刷新。</summary>
    private void OnEnable()
    {
        TrySubscribeEvents();
        RefreshAll();
    }

    /// <summary>取消事件订阅。</summary>
    private void OnDisable()
    {
        if (!isSubscribed)
        {
            return;
        }

        GameEvents.UnsubscribeBuffUnlockChanged(OnBuffUnlockChanged);
        GameEvents.UnsubscribeUpgradeCardGranted(OnUpgradeCardGranted);
        isSubscribed = false;
    }

    /// <summary>绑定子 Panel 回调。</summary>
    private void Awake()
    {
        if (pathPanel != null)
        {
            pathPanel.UnlockRequested += OnUnlockRequested;
        }
    }

    /// <summary>解绑子 Panel 回调。</summary>
    private void OnDestroy()
    {
        if (pathPanel != null)
        {
            pathPanel.UnlockRequested -= OnUnlockRequested;
        }
    }

    /// <summary>全量刷新路径与背包。</summary>
    public void RefreshAll()
    {
        if (!ServiceLocator.TryGet(out BuffUnlockService service))
        {
            SetStatus("Buff 解锁服务未就绪");
            pathPanel?.Refresh(null);
            cardBagPanel?.Refresh(null);
            return;
        }

        pathPanel?.Refresh(service.BuildAllSkillWindows());
        cardBagPanel?.Refresh(service.BuildInventorySlots());
    }

    /// <summary>设置底部状态提示。</summary>
    public void SetStatus(string message)
    {
        if (statusText == null)
        {
            return;
        }

        bool hasText = !string.IsNullOrWhiteSpace(message);
        statusText.gameObject.SetActive(hasText);
        statusText.text = hasText ? message : string.Empty;
        statusText.color = UiTechWastelandPalette.TextSecondary;
    }

    private void TrySubscribeEvents()
    {
        if (isSubscribed || !ServiceLocator.TryGet(out EventBus _))
        {
            return;
        }

        GameEvents.SubscribeBuffUnlockChanged(OnBuffUnlockChanged);
        GameEvents.SubscribeUpgradeCardGranted(OnUpgradeCardGranted);
        isSubscribed = true;
    }

    private void OnBuffUnlockChanged(GameEventContext ctx)
    {
        RefreshAll();
        if (ctx.Payload is BuffUnlockChangedEventArgs args)
        {
            SetStatus($"已解锁 {args.Kind} T{args.Tier}");
        }
    }

    private void OnUpgradeCardGranted(GameEventContext ctx)
    {
        RefreshAll();
    }

    private void OnUnlockRequested(SkillType skillType)
    {
        PlayUiSfx(GameConstants.AudioIds.SfxUiClick);
        if (!ServiceLocator.TryGet(out BuffUnlockService service))
        {
            SetStatus("Buff 解锁服务未就绪");
            return;
        }

        if (service.TryUnlockNext(skillType, out string message))
        {
            SetStatus(message);
            RefreshAll();
        }
        else
        {
            SetStatus(message);
        }
    }

    private static void PlayUiSfx(string sfxId)
    {
        if (!string.IsNullOrWhiteSpace(sfxId))
        {
            GameEvents.RaiseAudioPlaySfx(null, sfxId);
        }
    }
}
