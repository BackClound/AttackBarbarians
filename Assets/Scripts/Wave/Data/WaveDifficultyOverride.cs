using System;
using UnityEngine;

/// <summary>
/// 波次难度修正：在全局 <see cref="WaveProgressionConfigSO"/> 曲线之上叠加。
/// </summary>
[Serializable]
public struct WaveDifficultyOverride
{
    [Tooltip("敌人属性额外乘算（1 = 不变）。")]
    [SerializeField] private float statMultiplier;

    [Tooltip("刷怪间隔乘算（>1 更慢，<1 更快）。")]
    [SerializeField] private float spawnIntervalMultiplier;

    [Tooltip("单波最大刷怪数乘算。")]
    [SerializeField] private float maxSpawnCountMultiplier;

    [Tooltip("波次时长乘算。")]
    [SerializeField] private float waveDurationMultiplier;

    [Tooltip("精英怪概率加法修正。")]
    [SerializeField] private float eliteSpawnChanceAdd;

    [Tooltip("特殊怪概率加法修正。")]
    [SerializeField] private float specialSpawnChanceAdd;

    /// <summary>无修正。</summary>
    public static WaveDifficultyOverride Identity => default;

    /// <summary>仅设置属性乘算。</summary>
    public static WaveDifficultyOverride WithStatMultiplier(float mult)
    {
        return new WaveDifficultyOverride
        {
            statMultiplier = mult <= 0f ? 1f : mult
        };
    }

    /// <summary>敌人属性额外乘算。</summary>
    public float StatMultiplier => statMultiplier <= 0f ? 1f : statMultiplier;

    /// <summary>刷怪间隔乘算。</summary>
    public float SpawnIntervalMultiplier => spawnIntervalMultiplier <= 0f ? 1f : spawnIntervalMultiplier;

    /// <summary>单波最大刷怪数乘算。</summary>
    public float MaxSpawnCountMultiplier => maxSpawnCountMultiplier <= 0f ? 1f : maxSpawnCountMultiplier;

    /// <summary>波次时长乘算。</summary>
    public float WaveDurationMultiplier => waveDurationMultiplier <= 0f ? 1f : waveDurationMultiplier;

    /// <summary>精英怪概率加法修正。</summary>
    public float EliteSpawnChanceAdd => eliteSpawnChanceAdd;

    /// <summary>特殊怪概率加法修正。</summary>
    public float SpecialSpawnChanceAdd => specialSpawnChanceAdd;

    /// <summary>与另一修正叠加（乘法项相乘，加法项相加）。</summary>
    public WaveDifficultyOverride Combine(WaveDifficultyOverride other)
    {
        return new WaveDifficultyOverride
        {
            statMultiplier = StatMultiplier * other.StatMultiplier,
            spawnIntervalMultiplier = SpawnIntervalMultiplier * other.SpawnIntervalMultiplier,
            maxSpawnCountMultiplier = MaxSpawnCountMultiplier * other.MaxSpawnCountMultiplier,
            waveDurationMultiplier = WaveDurationMultiplier * other.WaveDurationMultiplier,
            eliteSpawnChanceAdd = EliteSpawnChanceAdd + other.EliteSpawnChanceAdd,
            specialSpawnChanceAdd = SpecialSpawnChanceAdd + other.SpecialSpawnChanceAdd
        };
    }
}
