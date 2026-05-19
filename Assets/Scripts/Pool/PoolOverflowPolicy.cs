/// <summary>
/// 对象池达到 <see cref="PoolEntry.MaxCount"/> 且不可扩容时的策略。
/// </summary>
public enum PoolOverflowPolicy
{
    /// <summary>在 <see cref="PoolEntry.CanExpand"/> 为 true 时创建新实例；否则拒绝。</summary>
    ExpandOrReject = 0,

    /// <summary>始终拒绝生成，返回 null。</summary>
    Reject = 1,

    /// <summary>强制回收当前最早借出的活跃实例，再借出新实例。</summary>
    RecycleOldest = 2
}
