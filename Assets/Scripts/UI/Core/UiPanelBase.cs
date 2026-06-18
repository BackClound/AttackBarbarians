using UnityEngine;

/// <summary>
/// UI 面板基类：管理显隐状态、进出场过渡动画与 PanelId 标识。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。每个全屏/弹层面板根物体挂一个派生组件。</para>
/// <para>子类在 <see cref="OnShow"/> / <see cref="OnHide"/> 中实现页面专属刷新逻辑。</para>
/// </remarks>
public abstract class UiPanelBase : MonoBehaviour
{
    [SerializeField] private string panelId;
    [SerializeField] private GameObject root;
    [SerializeField] private UiPanelTransition transition;
    [SerializeField] private bool useTransition = true;

    /// <summary>面板唯一标识，供 <see cref="UIManager"/> 注册与事件路由。</summary>
    public string PanelId => panelId;

    /// <summary>当前面板是否处于可见状态。</summary>
    public bool IsVisible { get; private set; }

    /// <summary>初始化根节点引用、过渡组件；若场景中为隐藏态则仅同步视觉，避免在首次 Show 激活时反注册。</summary>
    protected virtual void Awake()
    {
        if (root == null)
        {
            root = gameObject;
        }

        if (transition == null)
        {
            transition = GetComponent<UiPanelTransition>();
        }

        IsVisible = root != null && root.activeSelf;
        if (!IsVisible && transition != null)
        {
            transition.SnapHidden();
        }
    }

    private void EnsureTransitionReference()
    {
        if (transition == null)
        {
            transition = GetComponent<UiPanelTransition>();
        }
    }

    /// <summary>根节点是否处于激活显示（用于纠正 <see cref="IsVisible"/> 与场景状态不同步）。</summary>
    public bool IsRootActive => root != null && root.activeSelf;

    /// <summary>显示面板：激活根节点、触发 <see cref="OnShow"/> 并播放进场动画。</summary>
    public virtual void Show()
    {
        if (IsVisible && IsRootActive)
        {
            return;
        }

        EnsureRootActive();
        IsVisible = true;

        OnShow();
        EnsureTransitionReference();
        if (useTransition && transition != null)
        {
            transition.PlayShow();
        }
        else if (transition != null)
        {
            transition.SnapShown();
        }
    }

    /// <summary>隐藏面板：触发 <see cref="OnHide"/> 并播放退场动画后停用根节点。</summary>
    public virtual void Hide()
    {
        if (!IsVisible && !IsRootActive)
        {
            return;
        }

        IsVisible = false;
        OnHide();

        if (useTransition && transition != null && IsRootActive)
        {
            transition.PlayHide(DeactivateRoot);
        }
        else
        {
            ForceHideImmediate();
        }
    }

    /// <summary>立即隐藏面板，跳过退场动画（流程兜底用）。</summary>
    public void ForceHideImmediate()
    {
        IsVisible = false;
        EnsureTransitionReference();
        if (transition != null)
        {
            transition.SnapHidden();
        }

        DeactivateRoot();
    }

    private void EnsureRootActive()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (root != null && !root.activeSelf)
        {
            root.SetActive(true);
        }
    }

    private void DeactivateRoot()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    /// <summary>面板显示时的回调，子类用于刷新展示数据。</summary>
    protected virtual void OnShow() { }

    /// <summary>面板隐藏时的回调，子类用于清理订阅或重置状态。</summary>
    protected virtual void OnHide() { }

    /// <summary>立即设置面板可见性，跳过过渡动画。</summary>
    /// <param name="visible">为 <c>true</c> 时显示，为 <c>false</c> 时隐藏。</param>
    protected void SetVisibleImmediate(bool visible)
    {
        IsVisible = visible;
        if (root != null)
        {
            root.SetActive(visible);
        }

        if (transition == null)
        {
            return;
        }

        if (visible)
        {
            transition.SnapShown();
        }
        else
        {
            transition.SnapHidden();
        }
    }

    /// <summary>播放 UI 音效，通过 <see cref="GameEvents"/> 派发。</summary>
    /// <param name="sfxId">音效配置 ID；为空时不播放。</param>
    protected void PlayUiSfx(string sfxId)
    {
        if (!string.IsNullOrWhiteSpace(sfxId))
        {
            GameEvents.RaiseAudioPlaySfx(this, sfxId);
        }
    }
}
