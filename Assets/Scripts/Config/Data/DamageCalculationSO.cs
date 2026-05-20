using UnityEngine;

/// <summary>
/// 伤害公式与平衡参数（护甲、暴击、元素附加比例）。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject 资产）。</para>
/// <para><b>默认资产：</b><c>Assets/Resources/Config/Damage/DamageCalculation_Default.asset</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "DamageCalculation", menuName = "Attack Barbarians/Config/Damage Calculation")]
public class DamageCalculationSO : ScriptableObject
{
    [Header("Armor")]
    [Tooltip("有效护甲 = max(0, 目标护甲 - 攻击方破甲)。最终伤害 *= 100 / (100 + 有效护甲)。")]
    [SerializeField] private float armorDenominatorBase = 100f;

    [Header("Critical")]
    [SerializeField] private float defaultCritPower = 0.5f;
    [Tooltip("DOT 是否允许暴击。")]
    [SerializeField] private bool allowDotCritical;

    [Header("Element")]
    [Tooltip("元素附加伤害占攻击方对应元素 Stat 的比例（叠在基础伤害之后）。")]
    [SerializeField] private float elementStatScale = 1f;

    public float ArmorDenominatorBase => Mathf.Max(1f, armorDenominatorBase);
    public float DefaultCritPower => Mathf.Max(0f, defaultCritPower);
    public bool AllowDotCritical => allowDotCritical;
    public float ElementStatScale => Mathf.Max(0f, elementStatScale);

    public float ApplyArmor(float damage, float effectiveArmor)
    {
        if (effectiveArmor <= 0f)
        {
            return damage;
        }

        return damage * (ArmorDenominatorBase / (ArmorDenominatorBase + effectiveArmor));
    }
}
