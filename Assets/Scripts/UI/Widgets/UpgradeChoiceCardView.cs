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

    private void Awake()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnClick);
        }
    }

    private void OnDestroy()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(OnClick);
        }
    }

    public void Bind(int index, System.Action<int> selectedCallback)
    {
        choiceIndex = index;
        onSelected = selectedCallback;
    }

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

    public void Clear()
    {
        choiceIndex = -1;
        onSelected = null;
        gameObject.SetActive(false);
    }

    private void OnClick()
    {
        if (choiceIndex >= 0)
        {
            onSelected?.Invoke(choiceIndex);
        }
    }
}
