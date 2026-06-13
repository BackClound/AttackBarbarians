using UnityEngine;

/// <summary>
/// 敌人血量：护盾吸收、击杀事件发布与对象池重置。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在敌人 Prefab 根节点（与 <see cref="Enemy"/> 同物体）。</para>
/// </remarks>
public class Enemy_Health : Entity_Health
{
    private Enemy enemy;
    private EnemyController controller;
    [SerializeField] private float currentHp;
    [SerializeField] private float realHp;
    [SerializeField] private bool isDead;

    private object lastDamageSource;

    /// <summary>当前显示血量。</summary>
    public float CurrentHp => currentHp;
    /// <summary>最大血量（来自 Entity_Stats）。</summary>
    public float MaxHp => entity_Stats != null ? entity_Stats.GetMaxHp() : currentHp;

    /// <summary>
    /// 缓存敌人与控制器引用。
    /// </summary>
    public override void Awake()
    {
        base.Awake();
        enemy = GetComponent<Enemy>();
        controller = GetComponent<EnemyController>();
    }

    /// <summary>
    /// 非池化路径下初始化满血；池化由 <see cref="ResetForPool"/> 处理。
    /// </summary>
    private void Start()
    {
        if (controller != null && controller.IsReady)
        {
            return;
        }

        currentHp = entity_Stats.GetMaxHp();
        realHp = currentHp;
    }

    /// <summary>
    /// 记录最近一次伤害来源（用于击杀事件）。
    /// </summary>
    /// <param name="source">伤害来源对象。</param>
    public void SetLastDamageSource(object source)
    {
        lastDamageSource = source;
    }

    /// <summary>
    /// 是否仍可受到伤害。
    /// </summary>
    /// <returns>存活且未标记死亡时为 <c>true</c>。</returns>
    public override bool CanBeDamage()
    {
        return currentHp > 0 && !isDead;
    }

    /// <summary>
    /// 伤害应用前记录来源。
    /// </summary>
    /// <param name="info">伤害上下文。</param>
    /// <param name="result">结算结果。</param>
    protected override void OnBeforeDamageApplied(DamageInfo info, DamageResult result)
    {
        if (info.Source != null)
        {
            lastDamageSource = info.Source;
        }
    }

    /// <summary>
    /// 应用已结算伤害，含护盾吸收逻辑。
    /// </summary>
    /// <param name="result">伤害结算结果。</param>
    /// <param name="info">伤害上下文。</param>
    public override void ApplyResolvedDamage(DamageResult result, DamageInfo info)
    {
        if (result.FinalDamage <= 0f || !CanBeDamage())
        {
            return;
        }

        float finalDamage = result.FinalDamage;
        float mitigated = result.MitigatedAmount;
        if (TryGetComponent(out EnemyDamageShield shield) && shield.IsActive)
        {
            float absorbed = finalDamage - shield.AbsorbDamage(finalDamage);
            mitigated += absorbed;
        }

        var adjusted = new DamageResult(finalDamage, mitigated, result.IsCritical, result.IsKill, result.TriggeredTags);
        OnBeforeDamageApplied(info, adjusted);
        if (finalDamage > 0f)
        {
            ReduceHp(finalDamage);
        }
    }

    /// <summary>
    /// 扣减血量并在归零时触发死亡。
    /// </summary>
    /// <param name="damage">伤害量。</param>
    protected override void ReduceHp(float damage)
    {
        currentHp -= damage;

        if (currentHp <= 0 && !isDead)
        {
            isDead = true;
            Die();
            lastDamageSource = null;
        }
    }

    /// <summary>
    /// 预扣真实血量（用于伤害预览等扩展）。
    /// </summary>
    /// <param name="damage">预扣伤害量。</param>
    public void WillReduceHp(float damage)
    {
        realHp -= damage;
    }

    /// <summary>
    /// 回复血量。
    /// </summary>
    /// <param name="healing">治疗量。</param>
    public override void RaiseHp(float healing)
    {
        var newHp = currentHp += healing;
        currentHp = Mathf.Min(newHp, entity_Stats.GetMaxHp());
        realHp = Mathf.Min(realHp + healing, currentHp);
    }

    /// <summary>
    /// 获取敌人总攻击力。
    /// </summary>
    /// <returns>来自 Entity_Stats 的总伤害值。</returns>
    public float GetDamage()
    {
        return entity_Stats.GetTotalDamage();
    }

    /// <summary>
    /// 敌人死亡：通知能力组件、发布击杀事件并切换至死亡状态。
    /// </summary>
    public override void Die()
    {
        controller?.NotifyDeath();

        int experienceReward = controller != null ? controller.GetExperienceReward() : 0;
        GameEvents.RaiseEnemyKilled(enemy, new EnemyEventArgs(
            enemy.gameObject,
            enemy.transform.position,
            lastDamageSource,
            controller != null ? controller.ConfigId : string.Empty,
            experienceReward));

        enemy.stateMachine.ChangeState(enemy.deadState);
    }

    /// <summary>
    /// 对象池复用前重置血量与死亡标记。
    /// </summary>
    public void ResetForPool()
    {
        isDead = false;
        lastDamageSource = null;
        currentHp = entity_Stats.GetMaxHp();
        realHp = currentHp;
    }
}
