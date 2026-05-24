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

    public abstract EnemyAbilityTag Tag { get; }

    public void Configure(string configId)
    {
        abilityConfigId = configId ?? string.Empty;
    }

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

    public void OnDeath(EnemyController controller)
    {
        OnAbilityDeath(controller);
    }

    protected EnemyController Owner => owner;
    protected Enemy EnemyRef => enemy;
    protected SpecialEnemyAbilityDataSO Config => config;

    protected virtual void OnAbilitySpawn() { }

    protected virtual void TickActive(float deltaTime) { }

    protected virtual void OnAbilityDeath(EnemyController controller) { }

    protected virtual bool CanExecute() => true;

    protected abstract bool TryExecute();

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
