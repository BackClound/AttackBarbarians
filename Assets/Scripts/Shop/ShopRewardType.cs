/// <summary>
/// 商店商品购买后发放的资源类型。
/// </summary>
public enum ShopRewardType
{
    /// <summary>金币。</summary>
    Gold = 0,

    /// <summary>钻石（水晶）。</summary>
    Diamond = 1,

    /// <summary>体力（映射为广告券）。</summary>
    Energy = 2,

    /// <summary>广告券。</summary>
    AdTicket = 3,

    /// <summary>科技点。</summary>
    TechPoint = 4,

    /// <summary>升级卡（需配合卡池配置）。</summary>
    UpgradeCard = 5,
}
