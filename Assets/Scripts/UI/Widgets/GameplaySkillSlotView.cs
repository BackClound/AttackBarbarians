using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 局内已解锁技能槽：图标、等级、冷却遮罩。
/// </summary>
public class GameplaySkillSlotView : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TMP_Text cooldownText;

    /// <summary>清空槽位并隐藏。</summary>
    public void SetEmpty()
    {
        gameObject.SetActive(false);
    }

    /// <summary>根据技能运行时数据刷新展示。</summary>
    /// <param name="runtime">技能运行时；为 null 时隐藏。</param>
    public void SetData(SkillRuntime runtime)
    {
        if (runtime == null || !runtime.IsUnlocked || runtime.Config == null)
        {
            SetEmpty();
            return;
        }

        gameObject.SetActive(true);
        SkillDataSO config = runtime.Config;

        if (iconImage != null)
        {
            iconImage.sprite = config.Icon;
            iconImage.enabled = config.Icon != null;
        }

        int level = runtime.BaseData != null ? runtime.BaseData.Level : 1;
        if (PlayerSceneAccess.TryGetSkillSystem(out SkillManager skillManager))
        {
            level = skillManager.GetDisplayLevel(config.SkillType);
        }

        if (levelText != null)
        {
            levelText.text = $"{level}级";
            levelText.color = UiTechWastelandPalette.TextPrimary;
        }

        float cooldown = runtime.GetEffectiveCooldown();
        float elapsed = Mathf.Max(0f, Time.time - runtime.LastCastTime);
        float ratio = cooldown > 0.01f ? Mathf.Clamp01(elapsed / cooldown) : 1f;

        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = 1f - ratio;
            cooldownOverlay.enabled = ratio < 0.999f;
        }

        if (cooldownText != null)
        {
            if (ratio < 0.999f)
            {
                float remain = Mathf.Max(0f, cooldown - elapsed);
                cooldownText.text = remain >= 1f
                    ? $"{Mathf.CeilToInt(remain)}"
                    : remain.ToString("0.0");
                cooldownText.gameObject.SetActive(true);
            }
            else
            {
                cooldownText.gameObject.SetActive(false);
            }
        }
    }
}
