/// <summary>
/// 局内随机事件触发时机。
/// </summary>
public enum GameplayEventTriggerType
{
    /// <summary>地图加载完成时。</summary>
    OnMapLoad = 0,
    /// <summary>波次开始时。</summary>
    OnWaveStarted = 1,
    /// <summary>波次完成时。</summary>
    OnWaveCompleted = 2,
    /// <summary>波次进行中随机触发。</summary>
    RandomDuringWave = 3
}
