using System;
using UnityEngine;

/// <summary>
/// 精确波次覆盖：对单个波次序号打补丁，优先级高于段定义。
/// </summary>
[Serializable]
public class WaveExactOverride
{
    [SerializeField] private int waveIndex = 1;
    [SerializeField] private WaveSegmentDefinition patch = new WaveSegmentDefinition();

    /// <summary>目标波次序号。</summary>
    public int WaveIndex => Mathf.Max(1, waveIndex);

    /// <summary>覆盖内容（仅非空字段生效）。</summary>
    public WaveSegmentDefinition Patch => patch;
}
