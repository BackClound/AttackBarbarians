using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 资源条槽位：图标、数值、补充按钮；可选副标题（如体力恢复倒计时）。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。顶部资源 Pill Prefab 根物体。</para>
/// </remarks>
public class UI_ItemSlot : MonoBehaviour
{
    [SerializeField] private CurrencyType currency;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private TMP_Text subValueText;
    [SerializeField] private Button addButton;

    /// <summary>本槽位绑定的货币类型。</summary>
    public CurrencyType Currency => currency;

    /// <summary>点击加号按钮时触发，携带货币类型。</summary>
    public event Action<CurrencyType> AddClicked;

    /// <summary>绑定加号按钮点击事件。</summary>
    private void Awake()
    {
        if (addButton != null)
        {
            addButton.onClick.AddListener(OnAddClick);
        }
    }

    /// <summary>解绑加号按钮点击事件。</summary>
    private void OnDestroy()
    {
        if (addButton != null)
        {
            addButton.onClick.RemoveListener(OnAddClick);
        }
    }

    /// <summary>以千分位格式显示数值。</summary>
    /// <param name="amount">资源数量。</param>
    public void SetValue(long amount)
    {
        SetValue(amount.ToString("N0", CultureInfo.InvariantCulture));
    }

    /// <summary>以自定义字符串显示主数值。</summary>
    /// <param name="display">展示文本。</param>
    public void SetValue(string display)
    {
        if (valueText != null)
        {
            valueText.text = display ?? string.Empty;
        }
    }

    /// <summary>设置副标题（如体力恢复倒计时），无内容时隐藏。</summary>
    /// <param name="display">副标题文本；为空时隐藏。</param>
    public void SetSubValue(string display)
    {
        if (subValueText == null)
        {
            return;
        }

        bool hasText = !string.IsNullOrEmpty(display);
        subValueText.gameObject.SetActive(hasText);
        subValueText.text = hasText ? display : string.Empty;
    }

    /// <summary>加号按钮回调，向上层抛出货币类型。</summary>
    private void OnAddClick()
    {
        AddClicked?.Invoke(currency);
    }
}
