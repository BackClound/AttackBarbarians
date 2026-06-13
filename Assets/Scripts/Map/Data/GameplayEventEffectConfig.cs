using UnityEngine;

/// <summary>
/// 局内随机事件单条效果配置。
/// </summary>
[System.Serializable]
public struct GameplayEventEffectConfig
{
    [SerializeField] private GameplayEventEffectType effectType;
    [SerializeField] private float value;
    [SerializeField] private string stringParam;

    /// <summary>效果类型。</summary>
    public GameplayEventEffectType EffectType => effectType;
    /// <summary>数值参数（倍率等）。</summary>
    public float Value => value;
    /// <summary>字符串参数（如 Buff configId）。</summary>
    public string StringParam => stringParam ?? string.Empty;
}
