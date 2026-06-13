/// <summary>
/// 从 ScriptableObject 创建运行时数据，避免运行时修改 SO。
/// </summary>
public static class ConfigRuntimeFactory
{
    /// <summary>
    /// 从 <see cref="PlayerDataSO"/> 创建玩家运行时数据副本。
    /// </summary>
    /// <param name="source">来源玩家配置资产。</param>
    /// <param name="level">初始等级；-1 时使用配置中的起始等级。</param>
    /// <returns>已初始化的玩家运行时数据实例。</returns>
    public static PlayerRuntimeData CreatePlayer(PlayerDataSO source, int level = -1)
    {
        var runtime = new PlayerRuntimeData();
        runtime.Initialize(source, level);
        return runtime;
    }

    /// <summary>
    /// 从 <see cref="EnemyDataSO"/> 创建敌人运行时数据副本。
    /// </summary>
    /// <param name="source">来源敌人配置资产。</param>
    /// <returns>已初始化的敌人运行时数据实例。</returns>
    public static EnemyRuntimeData CreateEnemy(EnemyDataSO source)
    {
        var runtime = new EnemyRuntimeData();
        runtime.Initialize(source);
        return runtime;
    }

    /// <summary>
    /// 从 <see cref="SkillDataSO"/> 创建技能运行时数据副本。
    /// </summary>
    /// <param name="source">来源技能配置资产。</param>
    /// <param name="level">技能等级，默认为 1。</param>
    /// <returns>已初始化的技能运行时数据实例。</returns>
    public static SkillRuntimeData CreateSkill(SkillDataSO source, int level = 1)
    {
        var runtime = new SkillRuntimeData();
        runtime.Initialize(source, level);
        return runtime;
    }

    /// <summary>
    /// 从 <see cref="BuffDataSO"/> 创建 Buff 运行时数据副本。
    /// </summary>
    /// <param name="source">来源 Buff 配置资产。</param>
    /// <param name="stacks">初始层数，默认为 1。</param>
    /// <returns>已初始化的 Buff 运行时数据实例。</returns>
    public static BuffRuntimeData CreateBuff(BuffDataSO source, int stacks = 1)
    {
        var runtime = new BuffRuntimeData();
        runtime.Initialize(source, stacks);
        return runtime;
    }
}
