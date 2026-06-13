/// <summary>
/// 奖池预览单条：卡片、权重与爆率百分比。
/// </summary>
public readonly struct UpgradeCardPoolPreviewEntry
{
    /// <summary>卡片配置 ID。</summary>
    public string CardConfigId { get; }
    /// <summary>卡片配置引用。</summary>
    public UpgradeCardSO Card { get; }
    /// <summary>展开后的有效权重。</summary>
    public int Weight { get; }
    /// <summary>爆率百分比（0–100）。</summary>
    public float DropRatePercent { get; }

    /// <summary>创建奖池预览条目。</summary>
    /// <param name="cardConfigId">卡片配置 ID。</param>
    /// <param name="card">卡片配置。</param>
    /// <param name="weight">有效权重。</param>
    /// <param name="dropRatePercent">爆率百分比。</param>
    public UpgradeCardPoolPreviewEntry(string cardConfigId, UpgradeCardSO card, int weight, float dropRatePercent)
    {
        CardConfigId = cardConfigId ?? string.Empty;
        Card = card;
        Weight = weight;
        DropRatePercent = dropRatePercent;
    }
}
