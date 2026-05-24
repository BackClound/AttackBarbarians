using UnityEngine;

/// <summary>
/// UI 面板基类：显隐、动画与 PanelId。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。每个全屏/弹层面板根物体一个派生组件。</para>
/// </remarks>
public abstract class UiPanelBase : MonoBehaviour
{
    [SerializeField] private string panelId;
    [SerializeField] private GameObject root;
    [SerializeField] private UiPanelTransition transition;
    [SerializeField] private bool useTransition = true;

    public string PanelId => panelId;
    public bool IsVisible { get; private set; }

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

        SetVisibleImmediate(false);
    }

    public virtual void Show()
    {
        if (IsVisible)
        {
            return;
        }

        IsVisible = true;
        if (root != null)
        {
            root.SetActive(true);
        }

        OnShow();
        if (useTransition && transition != null)
        {
            transition.PlayShow();
        }
        else if (transition != null)
        {
            transition.SnapShown();
        }
    }

    public virtual void Hide()
    {
        if (!IsVisible)
        {
            return;
        }

        IsVisible = false;
        OnHide();

        if (useTransition && transition != null)
        {
            transition.PlayHide(() =>
            {
                if (root != null)
                {
                    root.SetActive(false);
                }
            });
        }
        else
        {
            SetVisibleImmediate(false);
        }
    }

    protected virtual void OnShow() { }

    protected virtual void OnHide() { }

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

    protected void PlayUiSfx(string sfxId)
    {
        if (!string.IsNullOrWhiteSpace(sfxId))
        {
            GameEvents.RaiseAudioPlaySfx(this, sfxId);
        }
    }
}
