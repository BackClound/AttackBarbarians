/// <summary>
/// 升级抽取候选：选项引用与池内有效权重。
/// </summary>
internal readonly struct UpgradeRollCandidate
{
    /// <summary>候选升级选项。</summary>
    public UpgradeOptionSO Option { get; }
    /// <summary>池内有效权重。</summary>
    public float Weight { get; }

    /// <summary>创建升级抽取候选。</summary>
    /// <param name="option">升级选项。</param>
    /// <param name="weight">有效权重。</param>
    public UpgradeRollCandidate(UpgradeOptionSO option, float weight)
    {
        Option = option;
        Weight = weight;
    }
}
