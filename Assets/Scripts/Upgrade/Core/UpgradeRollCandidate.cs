/// <summary>
/// 升级抽取候选：选项引用与池内有效权重。
/// </summary>
internal readonly struct UpgradeRollCandidate
{
    public UpgradeOptionSO Option { get; }
    public float Weight { get; }

    public UpgradeRollCandidate(UpgradeOptionSO option, float weight)
    {
        Option = option;
        Weight = weight;
    }
}
