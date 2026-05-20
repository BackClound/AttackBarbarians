/// <summary>
/// 敌人特殊能力接口：冲锋、护盾、分裂、召唤、远程等按标签挂载实现。
/// </summary>
public interface IEnemyAbility
{
    EnemyAbilityTag Tag { get; }

    void OnSpawn(EnemyController controller);

    void OnUpdate(EnemyController controller, float deltaTime);

    void OnDeath(EnemyController controller);
}
