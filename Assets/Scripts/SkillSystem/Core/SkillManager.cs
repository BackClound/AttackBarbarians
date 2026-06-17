using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家技能调度：解锁、冷却、Buff 聚合、自动释放与射击适配。
/// 流水线入口：冷却就绪 → 遍历 <see cref="ISkillEffect"/> → 伤害/投射物 → Buff/DOT → 事件/UI。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 Player 上，与 <see cref="PlayerSkillManager"/> 同物体。</para>
/// </remarks>
[DisallowMultipleComponent]
[RequireComponent(typeof(Player))]
public class SkillManager : MonoBehaviour
{
    [Header("Cast")]
    [SerializeField] private Transform castOrigin;
    [SerializeField] private bool enableAutoCast = true;

    [Header("Default Skills")]
    [SerializeField] private string shootSkillConfigId = GameConstants.ConfigIds.SkillShoot;
    [SerializeField] private bool unlockShootOnStart = true;

    private readonly Dictionary<SkillType, SkillRuntime> runtimes = new Dictionary<SkillType, SkillRuntime>(8);
    private readonly Dictionary<SkillType, ISkillEffect> effects = new Dictionary<SkillType, ISkillEffect>(8);
    private readonly Dictionary<SkillType, int> buffStackCounts = new Dictionary<SkillType, int>(16);
    private readonly List<SkillType> autoCastOrder = new List<SkillType>(8);
    private Player player;
    private PlayerController controller;
    private SkillContext context;
    private ShootSkillController shootController;
    private float healMinuteTimer;
    private float healPeakTimer;

    /// <summary>技能施法上下文（目标、伤害构建等）。</summary>
    public SkillContext Context => context;
    /// <summary>射击兼容控制器。</summary>
    public ShootSkillController ShootController => shootController;
    /// <summary>已注册技能运行时表（只读）。</summary>
    public IReadOnlyDictionary<SkillType, SkillRuntime> Runtimes => runtimes;

    /// <summary>缓存组件引用并注册技能效果类型。</summary>
    private void Awake()
    {
        player = GetComponent<Player>();
        controller = GetComponent<PlayerController>();
        shootController = GetComponent<ShootSkillController>();
        if (shootController == null)
        {
            shootController = gameObject.AddComponent<ShootSkillController>();
        }

        if (castOrigin == null)
        {
            castOrigin = transform;
        }

        RegisterEffectTypes();
    }

    /// <summary>构建上下文、默认解锁射击，并同步局外解锁。</summary>
    private void Start()
    {
        context = new SkillContext(player, controller, this, castOrigin);
        if (unlockShootOnStart)
        {
            UnlockSkill(shootSkillConfigId, 1);
        }

        if (ServiceLocator.TryGet(out SkillUnlockService unlockService))
        {
            unlockService.ApplyUnlocksToPlayerSkillManager();
        }

        BuffManager buffManager = GetComponent<BuffManager>();
        if (buffManager == null)
        {
            buffManager = GetComponentInChildren<BuffManager>();
        }

        if (ServiceLocator.TryGet(out SaveManager saveManager))
        {
            MetaProgressBuffBootstrap.TryApplyPermanentSkillBuffs(saveManager, buffManager);
        }

        PlaytestBootstrap.TryApplyStartupBuffs();
    }

    /// <summary>
    /// 每帧驱动被动治疗、自动施法循环。
    /// </summary>
    private void Update()
    {
        if (!enableAutoCast || context == null)
        {
            return;
        }

        float dt = Time.deltaTime;
        TickHealPassives(dt);
        if (!CanAutoCastNow())
        {
            return;
        }

        if (runtimes.TryGetValue(SkillType.Shoot, out SkillRuntime shootRuntime))
        {
            shootRuntime.SetCooldownDivisor(context.GetAttackSpeedMultiplier());
        }

        for (int i = 0; i < autoCastOrder.Count; i++)
        {
            SkillType type = autoCastOrder[i];

            if (!runtimes.TryGetValue(type, out SkillRuntime runtime) || !runtime.IsUnlocked)
            {
                continue;
            }

            if (!runtime.IsCooldownReady)
            {
                continue;
            }

            if (!effects.TryGetValue(type, out ISkillEffect effect))
            {
                continue;
            }

            if (runtime.Config != null && !runtime.Config.AutoCast)
            {
                continue;
            }

            if (effect.TryAutoCast(context, runtime))
            {
                runtime.StartCooldown();
            }
        }
    }

    /// <summary>
    /// 玩家是否处于可自动施法状态。
    /// </summary>
    /// <returns>控制器为空或已就绪时返回 true。</returns>
    private bool CanAutoCastNow() => controller == null || controller.IsReady;

    /// <summary>注册全部 <see cref="ISkillEffect"/> 实现到效果字典。</summary>
    private void RegisterEffectTypes()
    {
        effects.Clear();
        for (int t = 0; t <= (int)SkillType.Heal; t++)
        {
            SkillType type = (SkillType)t;
            ISkillEffect effect = SkillEffectFactory.Create(type);
            if (effect != null)
            {
                effects[type] = effect;
            }
        }
    }

    /// <summary>
    /// 获取指定技能的 Buff 聚合表。
    /// </summary>
    /// <param name="type">技能类型。</param>
    /// <returns>Buff 表；未解锁时返回 null。</returns>
    public SkillBuffProfile GetBuffProfile(SkillType type)
    {
        return runtimes.TryGetValue(type, out SkillRuntime runtime) ? runtime.BuffProfile : null;
    }

    /// <summary>
    /// 尝试获取技能运行时。
    /// </summary>
    /// <param name="type">技能类型。</param>
    /// <param name="runtime">输出的运行时。</param>
    /// <returns>是否存在该运行时。</returns>
    public bool TryGetRuntime(SkillType type, out SkillRuntime runtime) => runtimes.TryGetValue(type, out runtime);

    /// <summary>
    /// 指定类型技能是否已解锁。
    /// </summary>
    /// <param name="type">技能类型。</param>
    /// <returns>是否已解锁。</returns>
    public bool IsSkillUnlocked(SkillType type) =>
        runtimes.TryGetValue(type, out SkillRuntime runtime) && runtime.IsUnlocked;

    /// <summary>
    /// 按配置 ID 判断技能是否已解锁。
    /// </summary>
    /// <param name="configId">技能配置 ID。</param>
    /// <returns>是否已解锁。</returns>
    public bool IsSkillUnlocked(string configId)
    {
        if (string.IsNullOrWhiteSpace(configId) ||
            !ServiceLocator.TryGet(out ConfigManager configManager) ||
            !configManager.TryGetSkill(configId, out SkillDataSO data))
        {
            return false;
        }

        return IsSkillUnlocked(data.SkillType);
    }

    /// <summary>
    /// 按配置 ID 解锁技能。
    /// </summary>
    /// <param name="configId">技能配置 ID。</param>
    /// <param name="level">初始等级，默认 1。</param>
    /// <param name="persistToSave">是否写入存档，默认 true。</param>
    public void UnlockSkill(string configId, int level = 1, bool persistToSave = true)
    {
        if (!ServiceLocator.TryGet(out ConfigManager configManager) ||
            !configManager.TryGetSkill(configId, out SkillDataSO data))
        {
            Debug.LogWarning($"[SkillManager] 未找到技能配置: {configId}");
            return;
        }

        UnlockSkill(data, level, persistToSave);
    }

    /// <summary>
    /// 解锁或初始化指定技能配置。
    /// </summary>
    /// <param name="data">技能配置资产。</param>
    /// <param name="level">初始等级，默认 1。</param>
    /// <param name="persistToSave">是否写入存档，默认 true。</param>
    public void UnlockSkill(SkillDataSO data, int level = 1, bool persistToSave = true)
    {
        if (data == null)
        {
            return;
        }

        SkillType type = data.SkillType;
        if (!runtimes.TryGetValue(type, out SkillRuntime runtime))
        {
            runtime = new SkillRuntime();
            runtimes[type] = runtime;
            if (!autoCastOrder.Contains(type))
            {
                autoCastOrder.Add(type);
            }

            runtime.Initialize(data, level, true);
        }
        else if (runtime.IsUnlocked &&
                 runtime.Config != null &&
                 runtime.Config.ConfigId == data.ConfigId)
        {
            // 重复同步解锁（如 GameStarted + Start）时保留本局已叠加的 SkillBuffProfile。
            int newLevel = Mathf.Max(runtime.BaseData.Level, level);
            runtime.SetLevel(newLevel);
            runtime.Unlock();
        }
        else
        {
            runtime.Initialize(data, level, true);
        }
        if (persistToSave)
        {
            PersistUnlock(data.ConfigId, runtime.BaseData.Level);
        }

        GameEvents.RaiseSkillLevelUp(this, data.ConfigId, runtime.BaseData.Level);
    }

    /// <summary>
    /// 锁定指定技能（仅运行时生效，不修改存档）。
    /// </summary>
    /// <param name="configId">技能配置 ID。</param>
    public void LockSkill(string configId)
    {
        if (!ServiceLocator.TryGet(out ConfigManager configManager) ||
            !configManager.TryGetSkill(configId, out SkillDataSO data))
        {
            Debug.LogWarning($"[SkillManager] 未找到技能配置: {configId}");
            return;
        }

        SkillType type = data.SkillType;
        if (!runtimes.TryGetValue(type, out SkillRuntime runtime))
        {
            runtime = new SkillRuntime();
            runtimes[type] = runtime;
            runtime.Initialize(data, 1, false);
            return;
        }

        runtime.Lock();
    }

    /// <summary>
    /// 提升技能等级。
    /// </summary>
    /// <param name="configId">技能配置 ID。</param>
    /// <param name="delta">等级增量，默认 1。</param>
    /// <returns>是否升级成功。</returns>
    public bool UpgradeSkillLevel(string configId, int delta = 1)
    {
        if (delta <= 0 || !ServiceLocator.TryGet(out ConfigManager configManager) ||
            !configManager.TryGetSkill(configId, out SkillDataSO data))
        {
            return false;
        }

        SkillType type = data.SkillType;
        if (!runtimes.TryGetValue(type, out SkillRuntime runtime) || !runtime.IsUnlocked)
        {
            UnlockSkill(data, 1);
            runtime = runtimes[type];
        }

        int newLevel = Mathf.Min(data.MaxLevel, runtime.BaseData.Level + delta);
        runtime.SetLevel(newLevel);
        GameEvents.RaiseSkillLevelUp(this, data.ConfigId, newLevel);
        return true;
    }

    /// <summary>
    /// 射击技能是否可立即释放（冷却、解锁、有目标）。
    /// </summary>
    /// <returns>是否可射击。</returns>
    public bool CanShootNow() =>
        runtimes.TryGetValue(SkillType.Shoot, out SkillRuntime runtime) &&
        runtime.IsUnlocked &&
        runtime.IsCooldownReady &&
        context != null &&
        context.TryGetPrimaryTarget(out _);

    /// <summary>
    /// 应用技能 Buff（通用或指定技能）；写入 <see cref="SkillBuffProfile"/> 或玩家属性。
    /// </summary>
    /// <param name="kind">Buff 种类。</param>
    /// <param name="tier">Buff 层级，从 1 起。</param>
    public void ApplySkillBuff(SkillBuffKind kind, int tier = 1)
    {
        if (kind == SkillBuffKind.None)
        {
            return;
        }

        if (SkillBuffCatalog.IsGlobalKind(kind))
        {
            if (kind == SkillBuffKind.GlobalCooldownReduction)
            {
                foreach (var pair in runtimes)
                {
                    SkillBuffCatalog.Apply(pair.Value.BuffProfile, kind, tier);
                }
            }
            else
            {
                ApplyGlobalStatBuff(kind, tier);
            }

            GameEvents.RaiseBuffChanged(this, new BuffEventArgs(kind.ToString(), tier, 0f, player));
            return;
        }

        SkillType target = SkillBuffCatalog.GetTargetSkill(kind);
        if (!runtimes.TryGetValue(target, out SkillRuntime runtime))
        {
            Debug.LogWarning($"[SkillManager] 技能未解锁，无法应用 Buff: {kind} -> {target}");
            return;
        }

        SkillBuffCatalog.Apply(runtime.BuffProfile, kind, tier);
        if (kind == SkillBuffKind.HealMaxHpPercent && controller != null)
        {
            controller.ApplyModifier(new StatModifierConfig(
                StatType.MaxHp,
                ConfigModifierType.PercentAdd,
                SkillBuffCatalog.GetStackPercent(tier)));
        }

        if (!buffStackCounts.ContainsKey(target))
        {
            buffStackCounts[target] = 0;
        }

        buffStackCounts[target]++;
        GameEvents.RaiseBuffChanged(this, new BuffEventArgs(kind.ToString(), tier, 0f, player));
    }

    /// <summary>
    /// 从 <see cref="BuffDataSO"/> 配置应用 Buff（技能 Buff 或属性 Buff）。
    /// </summary>
    /// <param name="buff">Buff 配置。</param>
    /// <param name="stacks">堆叠层数，默认 1。</param>
    public void ApplyBuffFromConfig(BuffDataSO buff, int stacks = 1)
    {
        if (buff == null)
        {
            return;
        }

        if (buff.HasSkillBuff)
        {
            for (int i = 0; i < stacks; i++)
            {
                ApplySkillBuff(buff.SkillBuffKind, buff.SkillBuffTier);
            }

            return;
        }

        if (controller != null)
        {
            for (int i = 0; i < stacks; i++)
            {
                controller.ApplyBuff(buff, 1);
            }
        }
    }

    /// <summary>
    /// 将全局 Buff 写入玩家运行时属性（攻速、伤害、暴击等）。
    /// </summary>
    /// <param name="kind">全局 Buff 种类。</param>
    /// <param name="tier">Buff 层级。</param>
    private void ApplyGlobalStatBuff(SkillBuffKind kind, int tier)
    {
        if (controller == null)
        {
            return;
        }

        float pct = SkillBuffCatalog.GetStackPercent(tier);
        switch (kind)
        {
            case SkillBuffKind.GlobalAttackSpeed:
                controller.ApplyModifier(new StatModifierConfig(
                    StatType.AttackSpeedMulti, ConfigModifierType.PercentAdd, pct));
                break;
            case SkillBuffKind.GlobalBaseDamage:
                controller.ApplyModifier(new StatModifierConfig(
                    StatType.Damage, ConfigModifierType.PercentAdd, pct));
                break;
            case SkillBuffKind.GlobalCritChance:
                controller.ApplyModifier(new StatModifierConfig(
                    StatType.CritChance, ConfigModifierType.PercentAdd, pct));
                break;
            case SkillBuffKind.GlobalCritDamage:
                controller.ApplyModifier(new StatModifierConfig(
                    StatType.CritPower, ConfigModifierType.PercentAdd, pct));
                break;
            case SkillBuffKind.GlobalCooldownReduction:
                break;
        }
    }

    /// <summary>手动触发射击施法（兼容旧入口）。</summary>
    public void TriggerShootCast() => TryCastSkill(SkillType.Shoot);

    /// <summary>
    /// 尝试施放指定类型技能（不检查 AutoCast 开关）。
    /// </summary>
    /// <param name="type">技能类型。</param>
    /// <returns>是否成功施放。</returns>
    public bool TryCastSkill(SkillType type)
    {
        if (context == null ||
            !runtimes.TryGetValue(type, out SkillRuntime runtime) ||
            !runtime.IsUnlocked ||
            !effects.TryGetValue(type, out ISkillEffect effect))
        {
            return false;
        }

        if (effect.TryAutoCast(context, runtime))
        {
            runtime.StartCooldown();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 驱动恢复技能被动效果（每秒回血、定时治疗 Buff）。
    /// </summary>
    /// <param name="deltaTime">帧间隔秒数。</param>
    private void TickHealPassives(float deltaTime)
    {
        if (!runtimes.TryGetValue(SkillType.Heal, out SkillRuntime healRuntime) || !healRuntime.IsUnlocked)
        {
            return;
        }

        HealSkillEffect.TickPassive(context, healRuntime, deltaTime);
        SkillBuffProfile buff = healRuntime.BuffProfile;

        healMinuteTimer += deltaTime;
        if (buff.HealEveryMinuteTenPercent && healMinuteTimer >= 60f)
        {
            healMinuteTimer = 0f;
            float amount = context.Player.player_Health.MaxHp * 0.1f;
            context.Player.player_Health.Heal(amount);
        }

        healPeakTimer += deltaTime;
        if (buff.HealEvery3MinPeakTenPercent && healPeakTimer >= 180f)
        {
            healPeakTimer = 0f;
            if (controller != null && controller.RuntimeStats.IsInitialized)
            {
                controller.ApplyModifier(new StatModifierConfig(
                    StatType.MaxHp, ConfigModifierType.PercentAdd, 0.1f));
            }
        }
    }

    /// <summary>
    /// 将解锁等级持久化到存档（仅当新等级更高时写入）。
    /// </summary>
    /// <param name="configId">技能配置 ID。</param>
    /// <param name="level">当前等级。</param>
    private static void PersistUnlock(string configId, int level)
    {
        if (string.IsNullOrWhiteSpace(configId) || level <= 0)
        {
            return;
        }

        if (!ServiceLocator.TryGet(out SaveManager saveManager) || saveManager.Current == null)
        {
            return;
        }

        int existing = saveManager.Current.GetSkillLevel(configId);
        if (level > existing)
        {
            saveManager.Current.SetSkillLevel(configId, level);
            saveManager.MarkDirty();
        }
    }

    /// <summary>调试：解锁全部技能。</summary>
    [ContextMenu("Debug/Unlock All Skills")]
    private void DebugUnlockAll()
    {
        UnlockSkill(GameConstants.ConfigIds.SkillShoot, 1);
        UnlockSkill(GameConstants.ConfigIds.SkillLightning, 1);
        UnlockSkill(GameConstants.ConfigIds.SkillThunder, 1);
        UnlockSkill(GameConstants.ConfigIds.SkillFireRain, 1);
        UnlockSkill(GameConstants.ConfigIds.SkillWaterWave, 1);
        UnlockSkill(GameConstants.ConfigIds.SkillIce, 1);
        UnlockSkill(GameConstants.ConfigIds.SkillHeal, 1);
    }
}
