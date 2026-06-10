/// <summary>
/// 奖池预览单条：卡片、权重与爆率百分比。
/// </summary>
public readonly struct UpgradeCardPoolPreviewEntry
{
    public string CardConfigId { get; }
    public UpgradeCardSO Card { get; }
    public int Weight { get; }
    public float DropRatePercent { get; }

    public UpgradeCardPoolPreviewEntry(string cardConfigId, UpgradeCardSO card, int weight, float dropRatePercent)
    {
        CardConfigId = cardConfigId ?? string.Empty;
        Card = card;
        Weight = weight;
        DropRatePercent = dropRatePercent;
    }
}
