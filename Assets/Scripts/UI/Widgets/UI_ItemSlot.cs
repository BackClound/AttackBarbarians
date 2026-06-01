using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 资源条槽位：图标、数值、补充按钮。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。顶部资源 Pill Prefab 根物体。</para>
/// </remarks>
public class UI_ItemSlot : MonoBehaviour
{
    [SerializeField] private CurrencyType currency;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private Button addButton;

    public CurrencyType Currency => currency;

    public event Action<CurrencyType> AddClicked;

    private void Awake()
    {
        if (addButton != null)
        {
            addButton.onClick.AddListener(OnAddClick);
        }
    }

    private void OnDestroy()
    {
        if (addButton != null)
        {
            addButton.onClick.RemoveListener(OnAddClick);
        }
    }

    public void SetValue(long amount)
    {
        SetValue(amount.ToString("N0", CultureInfo.InvariantCulture));
    }

    public void SetValue(string display)
    {
        if (valueText != null)
        {
            valueText.text = display ?? string.Empty;
        }
    }

    private void OnAddClick()
    {
        AddClicked?.Invoke(currency);
    }
}
