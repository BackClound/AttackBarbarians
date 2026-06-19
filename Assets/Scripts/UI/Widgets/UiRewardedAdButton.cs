using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 激励广告按钮：Button + 文案 + 广告角标，供三选一刷新/全选等场景复用。
/// </summary>
public class UiRewardedAdButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Image adBadgeImage;
    [SerializeField] private TMP_Text adBadgeText;

    private Action onClicked;

    /// <summary>绑定点击回调。</summary>
    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    /// <summary>解绑点击回调。</summary>
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    /// <summary>设置主文案。</summary>
    /// <param name="text">按钮文字。</param>
    public void SetLabel(string text)
    {
        if (labelText != null)
        {
            labelText.text = text ?? string.Empty;
        }
    }

    /// <summary>设置广告角标文案（如「AD」）。</summary>
    /// <param name="text">角标文字。</param>
    public void SetAdBadge(string text)
    {
        if (adBadgeText != null)
        {
            adBadgeText.text = text ?? string.Empty;
        }
    }

    /// <summary>设置按钮可交互状态。</summary>
    /// <param name="interactable">是否可点击。</param>
    public void SetInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    /// <summary>绑定点击事件（每次调用会覆盖上一次）。</summary>
    /// <param name="callback">点击回调。</param>
    public void BindClick(Action callback)
    {
        onClicked = callback;
    }

    /// <summary>转发按钮点击到外部绑定回调。</summary>
    private void HandleClick()
    {
        onClicked?.Invoke();
    }
}