using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 升级三选一卡片：图标、名称、描述、稀有度边框。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。UpgradePanel 下每个选项槽位一个实例。</para>
/// </remarks>
public class UpgradeChoiceCardView : MonoBehaviour
{
    [SerializeField] private Button selectButton;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private UiRarityVisual rarityVisual;

    private int choiceIndex = -1;
    private System.Action<int> onSelected;

    /// <summary>绑定选择按钮点击事件。</summary>
    private void Awake()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnClick);
        }
    }

    /// <summary>解绑选择按钮点击事件。</summary>
    private void OnDestroy()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(OnClick);
        }
    }

    /// <summary>绑定选项索引与选中回调。</summary>
    /// <param name="index">三选一中的选项下标。</param>
    /// <param name="selectedCallback">玩家选中时的回调。</param>
    public void Bind(int index, System.Action<int> selectedCallback)
    {
        choiceIndex = index;
        onSelected = selectedCallback;
    }

    /// <summary>根据升级选项数据刷新卡片展示。</summary>
    /// <param name="option">升级选项配置；为 <c>null</c> 时隐藏卡片。</param>
    public void SetData(UpgradeOptionSO option)
    {
        if (option == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (titleText != null)
        {
            titleText.text = option.DisplayName;
            titleText.color = UiTechWastelandPalette.TextPrimary;
        }

        if (descriptionText != null)
        {
            descriptionText.text = option.Description;
            descriptionText.color = UiTechWastelandPalette.TextSecondary;
        }

        if (iconImage != null)
        {
            iconImage.sprite = option.Icon;
            iconImage.enabled = option.Icon != null;
        }

        if (rarityVisual != null)
        {
            rarityVisual.ApplyUpgradeRarity(option.Rarity);
        }
    }

    /// <summary>清空绑定并隐藏卡片。</summary>
    public void Clear()
    {
        choiceIndex = -1;
        onSelected = null;
        gameObject.SetActive(false);
    }

    /// <summary>选择按钮回调，通知 Presenter 玩家选择。</summary>
    private void OnClick()
    {
        if (choiceIndex >= 0)
        {
            onSelected?.Invoke(choiceIndex);
        }
    }
}
