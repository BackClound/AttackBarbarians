using UnityEngine;

/// <summary>
/// 特殊敌人能力基类：冷却、配置解析与 <see cref="GameEvents.RaiseSpecialEnemyAbilityUsed"/>。
/// </summary>
/// <remarks>由 <see cref="SpecialEnemyAbilityFactory"/> 在生成时挂载，通常无需手动拖到 Prefab。</remarks>
public abstract class EnemyAbilityBase : MonoBehaviour, IEnemyAbility
{
    [SerializeField] private string abilityConfigId;

    private EnemyController owner;
    private Enemy enemy;
    private SpecialEnemyAbilityDataSO config;
    private float cooldownTimer;
    private float activeTimer;
    private bool isReady;

    /// <summary>能力标签（由子类实现）。</summary>
    public abstract EnemyAbilityTag Tag { get; }

    /// <summary>设置能力配置 Id。</summary>
    /// <param name="configId">能力配置 Id。</param>
    public void Configure(string configId)
    {
        abilityConfigId = configId ?? string.Empty;
    }

    /// <summary>敌人生成时初始化冷却与配置。</summary>
    /// <param name="controller">所属敌人控制器。</param>
    public void OnSpawn(EnemyController controller)
    {
        owner = controller;
        enemy = controller != null ? controller.Enemy : null;
        config = ResolveConfig();
        cooldownTimer = config != null ? config.InitialCooldownSeconds : 999f;
        activeTimer = 0f;
        isReady = controller != null && config != null;
        OnAbilitySpawn();
    }

    /// <summary>每帧 Tick 冷却并尝试执行能力。</summary>
    /// <param name="controller">所属敌人控制器。</param>
    /// <param name="deltaTime">帧间隔（秒）。</param>
    public void OnUpdate(EnemyController controller, float deltaTime)
    {
        if (!isReady || config == null || enemy == null || enemy.enemy_Health == null)
        {
            return;
        }

        if (!enemy.enemy_Health.CanBeDamage())
        {
            return;
        }

        if (activeTimer > 0f)
        {
            activeTimer -= deltaTime;
            TickActive(deltaTime);
            return;
        }

        cooldownTimer -= deltaTime;
        if (cooldownTimer > 0f || !CanExecute())
        {
            return;
        }

        if (TryExecute())
        {
            cooldownTimer = config.CooldownSeconds;
            activeTimer = config.DurationSeconds;
            RaiseAbilityUsed();
        }
    }

    /// <summary>敌人死亡时回调。</summary>
    /// <param name="controller">所属敌人控制器。</param>
    public void OnDeath(EnemyController controller)
    {
        OnAbilityDeath(controller);
    }

    /// <summary>所属敌人控制器。</summary>
    protected EnemyController Owner => owner;
    /// <summary>所属敌人实体。</summary>
    protected Enemy EnemyRef => enemy;
    /// <summary>解析后的能力配置。</summary>
    protected SpecialEnemyAbilityDataSO Config => config;

    /// <summary>生成时的子类扩展点。</summary>
    protected virtual void OnAbilitySpawn() { }

    /// <summary>能力持续期间的子类 Tick。</summary>
    /// <param name="deltaTime">帧间隔（秒）。</param>
    protected virtual void TickActive(float deltaTime) { }

    /// <summary>死亡时的子类扩展点。</summary>
    /// <param name="controller">所属敌人控制器。</param>
    protected virtual void OnAbilityDeath(EnemyController controller) { }

    /// <summary>是否满足执行条件（子类可覆盖）。</summary>
    /// <returns>允许执行时为 <c>true</c>。</returns>
    protected virtual bool CanExecute() => true;

    /// <summary>执行能力逻辑（子类实现）。</summary>
    /// <returns>执行成功时为 <c>true</c>。</returns>
    protected abstract bool TryExecute();

    /// <summary>从 ConfigManager 解析能力配置。</summary>
    /// <returns>能力配置，失败时为 <c>null</c>。</returns>
    private SpecialEnemyAbilityDataSO ResolveConfig()
    {
        if (ServiceLocator.TryGet(out ConfigManager configManager))
        {
            if (!string.IsNullOrEmpty(abilityConfigId) &&
                configManager.TryGetSpecialEnemyAbility(abilityConfigId, out SpecialEnemyAbilityDataSO data))
            {
                return data;
            }

            string fallbackId = SpecialEnemyRules.GetDefaultAbilityConfigId(Tag);
            if (!string.IsNullOrEmpty(fallbackId) &&
                configManager.TryGetSpecialEnemyAbility(fallbackId, out data))
            {
                return data;
            }
        }

        return null;
    }

    /// <summary>发布特殊敌人能力使用事件。</summary>
    private void RaiseAbilityUsed()
    {
        if (owner == null || config == null)
        {
            return;
        }

        GameEvents.RaiseSpecialEnemyAbilityUsed(this, new SpecialEnemyAbilityUsedEventArgs(
            owner.gameObject,
            owner.ConfigId,
            Tag,
            config.ConfigId));
    }
}
