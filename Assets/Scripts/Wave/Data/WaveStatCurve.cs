using UnityEngine;

/// <summary>
/// 单属性随波次的成长曲线：乘法倍率或加法增量（暴击类）。
/// </summary>
[System.Serializable]
public struct WaveStatCurve
{
    [Tooltip("曲线系数 a。")]
    [SerializeField] private float coefficient;

    [Tooltip("曲线幂次 p。")]
    [SerializeField] private float power;

    [Tooltip("乘法倍率上限；0 表示不封顶。")]
    [SerializeField] private float maxMultiplier;

    [Tooltip("加法模式绝对上限；>0 时启用加法模式。")]
    [SerializeField] private float additiveCap;

    /// <summary>曲线系数。</summary>
    public float Coefficient => coefficient;

    /// <summary>曲线幂次。</summary>
    public float Power => power;

    /// <summary>乘法倍率上限（0 = 无上限）。</summary>
    public float MaxMultiplier => maxMultiplier;

    /// <summary>加法模式绝对上限。</summary>
    public float AdditiveCap => additiveCap;

    /// <summary>是否以加法模式应用（暴击率/暴伤）。</summary>
    public bool IsAdditive => additiveCap > 0f;

    /// <summary>构造乘法曲线。</summary>
    public static WaveStatCurve Multiplicative(float a, float p, float max = 0f) =>
        new WaveStatCurve
        {
            coefficient = a,
            power = p,
            maxMultiplier = max,
            additiveCap = 0f
        };

    /// <summary>构造加法曲线。</summary>
    public static WaveStatCurve Additive(float a, float p, float cap) =>
        new WaveStatCurve
        {
            coefficient = a,
            power = p,
            maxMultiplier = 0f,
            additiveCap = cap
        };
}
