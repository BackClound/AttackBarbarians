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

    public GameplayEventEffectType EffectType => effectType;
    public float Value => value;
    public string StringParam => stringParam ?? string.Empty;
}
