using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 升级三选一卡片：Buff 名称、描述、图标、稀有度边框。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。UpgradePanel 下每个选项槽位一个实例。</para>
/// </remarks>
public class UpgradeChoiceCardView : MonoBehaviour
{
    [Header("Buff Display")]
    [SerializeField] private TMP_Text buffNameText;
    [SerializeField] private TMP_Text buffDescriptionText;
    [SerializeField] private Image buffIconImage;

    [Header("Interaction")]
    [SerializeField] private Button selectButton;

    [Header("Optional")]
    [SerializeField] private TMP_Text learnHintText;
    [SerializeField] private GameObject newBadgeRoot;
    [SerializeField] private UiRarityVisual rarityVisual;

    [Header("Legacy Bindings (optional fallback)")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;

    private int choiceIndex = -1;
    private System.Action<int> onSelected;

    /// <summary>绑定选择按钮点击事件。</summary>
    private void Awake()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnClick);
        }

        ConfigureIconImage(buffIconImage ?? iconImage);
        rarityVisual?.EnsureOutlineBorderMode();
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
    public void Bind(int index, System.Action<int> selectedCallback)
    {
        choiceIndex = index;
        onSelected = selectedCallback;
    }

    /// <summary>根据升级选项数据刷新卡片展示。</summary>
    public void SetData(UpgradeOptionSO option)
    {
        if (option == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        UpgradeOptionPresentation presentation = UpgradeOptionPresentationResolver.Resolve(option);

        SetText(buffNameText ?? titleText, presentation.Name, UiTechWastelandPalette.TextPrimary);
        SetText(buffDescriptionText ?? descriptionText, presentation.Description, UiTechWastelandPalette.TextSecondary);
        ApplyIcon(buffIconImage ?? iconImage, presentation.Icon);

        if (learnHintText != null)
        {
            bool isUnlock = option.EffectType == UpgradeEffectType.SkillUnlock;
            learnHintText.gameObject.SetActive(isUnlock);
            if (isUnlock)
            {
                learnHintText.text = string.IsNullOrWhiteSpace(presentation.Description)
                    ? $"学习{presentation.Name}"
                    : presentation.Description;
                learnHintText.color = UiTechWastelandPalette.TextPrimary;
            }
        }

        if (newBadgeRoot != null)
        {
            newBadgeRoot.SetActive(option.EffectType == UpgradeEffectType.SkillUnlock);
        }

        if (rarityVisual != null)
        {
            rarityVisual.EnsureOutlineBorderMode();
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

    private static void SetText(TMP_Text text, string value, Color color)
    {
        if (text == null)
        {
            return;
        }

        text.text = value ?? string.Empty;
        text.color = color;
    }

    private static void ApplyIcon(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.enabled = sprite != null;
        image.preserveAspect = true;
    }

    private static void ConfigureIconImage(Image image)
    {
        if (image == null)
        {
            return;
        }

        image.preserveAspect = true;
        image.type = Image.Type.Simple;
    }

    private void OnClick()
    {
        if (choiceIndex >= 0)
        {
            onSelected?.Invoke(choiceIndex);
        }
    }
}
