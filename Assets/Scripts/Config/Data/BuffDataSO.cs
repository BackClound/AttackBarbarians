using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buff 配置：持续时间、堆叠与属性修正。
/// </summary>
[CreateAssetMenu(fileName = "BuffData", menuName = "Attack Barbarians/Config/Buff Data")]
public class BuffDataSO : ConfigDataBase
{
    [Header("Buff")]
    [SerializeField] private float duration = 5f;
    [SerializeField] private bool isPermanent;
    [SerializeField] private int maxStacks = 1;
    [SerializeField] private bool refreshDurationOnStack;

    [Header("Modifiers")]
    [SerializeField] private List<StatModifierConfig> modifiers = new List<StatModifierConfig>();

    [Header("Presentation")]
    [SerializeField] private string description;
    [SerializeField] private Color tintColor = Color.white;

    public float Duration => duration;
    public bool IsPermanent => isPermanent;
    public int MaxStacks => Mathf.Max(1, maxStacks);
    public bool RefreshDurationOnStack => refreshDurationOnStack;
    public IReadOnlyList<StatModifierConfig> Modifiers => modifiers;
    public string Description => description;
    public Color TintColor => tintColor;

    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (!isPermanent && duration <= 0f)
        {
            result.AddError(name, "非永久 Buff 的 duration 必须大于 0。");
        }

        if (maxStacks < 1)
        {
            result.AddError(name, "maxStacks 不能小于 1。");
        }

        if (Icon == null)
        {
            result.AddWarning(name, "未配置 Buff 图标。");
        }
    }
}
