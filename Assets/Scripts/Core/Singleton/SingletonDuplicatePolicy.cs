/// <summary>
/// 单例重复实例时的处理策略。
/// </summary>
/// <remarks>纯枚举，无需挂载。</remarks>
public enum SingletonDuplicatePolicy
{
    /// <summary>销毁后创建的重复组件所在物体（推荐，默认）。</summary>
    DestroyNewest = 0,

    /// <summary>销毁已存在的实例，由新实例接管（仅特殊迁移场景使用）。</summary>
    DestroyOldest = 1
}
