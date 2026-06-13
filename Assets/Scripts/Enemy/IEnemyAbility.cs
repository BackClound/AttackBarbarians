/// <summary>
/// 敌人特殊能力接口：冲锋、护盾、分裂、召唤、远程等按标签挂载实现。
/// </summary>
public interface IEnemyAbility
{
    /// <summary>能力对应的配置标签。</summary>
    EnemyAbilityTag Tag { get; }

    /// <summary>敌人从对象池取出并完成初始化时调用。</summary>
    /// <param name="controller">敌人运行时协调器。</param>
    void OnSpawn(EnemyController controller);

    /// <summary>每帧更新能力逻辑。</summary>
    /// <param name="controller">敌人运行时协调器。</param>
    /// <param name="deltaTime">帧间隔（秒）。</param>
    void OnUpdate(EnemyController controller, float deltaTime);

    /// <summary>敌人死亡时调用。</summary>
    /// <param name="controller">敌人运行时协调器。</param>
    void OnDeath(EnemyController controller);
}
