/// <summary>
/// SkillBuff 应用来源：区分局内临时成长与局外永久成长。
/// </summary>
public enum SkillBuffApplySource
{
    /// <summary>局内三选一 / 本局升级（计入 HUD 局内选取次数）。</summary>
    RunUpgrade = 0,

    /// <summary>局外永久成长（<see cref="SaveData.permanentUpgrades"/>）。</summary>
    MetaPermanent = 1,

    /// <summary>GameConfig 试玩开局 Buff。</summary>
    Playtest = 2,
}
