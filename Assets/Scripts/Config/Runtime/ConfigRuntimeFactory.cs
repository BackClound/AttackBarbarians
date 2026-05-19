/// <summary>
/// 从 ScriptableObject 创建运行时数据，避免运行时修改 SO。
/// </summary>
public static class ConfigRuntimeFactory
{
    public static PlayerRuntimeData CreatePlayer(PlayerDataSO source, int level = -1)
    {
        var runtime = new PlayerRuntimeData();
        runtime.Initialize(source, level);
        return runtime;
    }

    public static EnemyRuntimeData CreateEnemy(EnemyDataSO source)
    {
        var runtime = new EnemyRuntimeData();
        runtime.Initialize(source);
        return runtime;
    }

    public static SkillRuntimeData CreateSkill(SkillDataSO source, int level = 1)
    {
        var runtime = new SkillRuntimeData();
        runtime.Initialize(source, level);
        return runtime;
    }

    public static BuffRuntimeData CreateBuff(BuffDataSO source, int stacks = 1)
    {
        var runtime = new BuffRuntimeData();
        runtime.Initialize(source, stacks);
        return runtime;
    }
}
