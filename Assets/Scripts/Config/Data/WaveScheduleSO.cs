using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 波次表：用段定义 + 精确覆盖描述任意数量波次，无需逐波 WaveData 资产。
/// </summary>
/// <remarks>
/// <para><b>创建：</b>Attack Barbarians → Config → Wave Schedule。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/Wave/</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "WaveSchedule", menuName = "Attack Barbarians/Config/Wave Schedule")]
public class WaveScheduleSO : ConfigDataBase
{
    [Header("Procedural (Recommended)")]
    [Tooltip("启用后按 WaveProceduralRules 自动计算难度档、敌人解锁与 Boss。")]
    [SerializeField] private bool useProceduralRules = true;
    [SerializeField] private WaveProceduralRulesSO proceduralRules;

    [Header("Progression")]
    [Tooltip("留空则使用 ConfigDatabase 全局 WaveProgression。")]
    [SerializeField] private WaveProgressionConfigSO progressionOverride;

    [Tooltip("HUD 展示用波次总数；0 表示无限波次。")]
    [SerializeField] private int displayTotalWaves;

    [Header("Default")]
    [SerializeField] private WaveSegmentDefinition defaultSegment = new WaveSegmentDefinition();

    [Header("Segments")]
    [Tooltip("按 startWave 匹配；同波次命中多段时取 startWave 最大者。")]
    [SerializeField] private List<WaveSegmentDefinition> segments = new List<WaveSegmentDefinition>();

    [Header("Exact Overrides")]
    [Tooltip("对特定波次打补丁，优先级最高。")]
    [SerializeField] private List<WaveExactOverride> exactOverrides = new List<WaveExactOverride>();

    /// <summary>是否启用程序化波次规则。</summary>
    public bool UseProceduralRules => useProceduralRules;

    /// <summary>程序化波次规则配置。</summary>
    public WaveProceduralRulesSO ProceduralRules => proceduralRules;

    /// <summary>可选的波次成长曲线覆写。</summary>
    public WaveProgressionConfigSO ProgressionOverride => progressionOverride;

    /// <summary>HUD 展示用波次总数（0 = 无限）。</summary>
    public int DisplayTotalWaves => Mathf.Max(0, displayTotalWaves);

    /// <summary>默认段（无段匹配时的回退）。</summary>
    public WaveSegmentDefinition DefaultSegment => defaultSegment;

    /// <summary>波次段列表。</summary>
    public IReadOnlyList<WaveSegmentDefinition> Segments => segments;

    /// <summary>精确波次覆盖列表。</summary>
    public IReadOnlyList<WaveExactOverride> ExactOverrides => exactOverrides;

    /// <summary>
    /// 解析指定波次序号对应的段定义（含精确覆盖）。
    /// </summary>
    public WaveSegmentDefinition ResolveSegmentForWave(int waveIndex)
    {
        waveIndex = Mathf.Max(1, waveIndex);
        WaveSegmentDefinition resolved = defaultSegment != null
            ? defaultSegment.CloneForResolve()
            : new WaveSegmentDefinition();

        if (segments != null)
        {
            WaveSegmentDefinition best = null;
            int bestStart = int.MinValue;
            for (int i = 0; i < segments.Count; i++)
            {
                WaveSegmentDefinition segment = segments[i];
                if (segment == null || !segment.ContainsWave(waveIndex))
                {
                    continue;
                }

                if (segment.StartWave >= bestStart)
                {
                    bestStart = segment.StartWave;
                    best = segment;
                }
            }

            if (best != null)
            {
                resolved = best.CloneForResolve();
            }
        }

        if (exactOverrides != null)
        {
            for (int i = 0; i < exactOverrides.Count; i++)
            {
                WaveExactOverride exact = exactOverrides[i];
                if (exact != null && exact.WaveIndex == waveIndex && exact.Patch != null)
                {
                    resolved = resolved.MergePatch(exact.Patch);
                }
            }
        }

        return resolved;
    }

    /// <summary>收集波次表校验信息。</summary>
    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (useProceduralRules && proceduralRules != null)
        {
            result.AddWarning(name, "已启用程序化规则，segments/exactOverrides 将被忽略。");
        }

        if (useProceduralRules && proceduralRules == null)
        {
            result.AddError(name, "useProceduralRules 为 true 但 proceduralRules 未配置。");
        }

        if (defaultSegment == null)
        {
            result.AddWarning(name, "defaultSegment 为空。");
        }

        if (segments != null)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                WaveSegmentDefinition segment = segments[i];
                if (segment == null)
                {
                    result.AddWarning(name, $"segments[{i}] 为空。");
                    continue;
                }

                if (segment.EndWave > 0 && segment.EndWave < segment.StartWave)
                {
                    result.AddError(name, $"segments[{i}] endWave 小于 startWave。");
                }
            }
        }
    }
}
