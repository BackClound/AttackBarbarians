using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家技能调度：解锁、冷却、Buff 聚合、自动释放与射击适配。
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
    // 
    private readonly List<SkillType> autoCastOrder = new List<SkillType>(8);
    private Player player;
    private PlayerController controller;
    private SkillContext context;
    // 
    private ShootBurstController shootBurst;
    private ShootSkillController shootController;
    private float healMinuteTimer;
    private float healPeakTimer;

    public SkillContext Context => context;
    public ShootBurstController ShootBurst => shootBurst;
    public ShootSkillController ShootController => shootController;
    public IReadOnlyDictionary<SkillType, SkillRuntime> Runtimes => runtimes;

    private void Awake()
    {
        player = GetComponent<Player>();
        controller = GetComponent<PlayerController>();
        shootBurst = GetComponent<ShootBurstController>();
        if (shootBurst == null)
        {
            shootBurst = gameObject.AddComponent<ShootBurstController>();
        }

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
    }

    private void Update()
    {
        if (!enableAutoCast || context == null)
        {
            return;
        }

        float dt = Time.deltaTime;
        shootBurst?.Tick(dt);
        TickHealPassives(dt);
        if (!CanAutoCastNow())
        {
            return;
        }

        for (int i = 0; i < autoCastOrder.Count; i++)
        {
            SkillType type = autoCastOrder[i];

            // 射击由 SkillShoot 敌人检测驱动，避免与 SkillManager 自动施法双发。
            if (type == SkillType.Shoot)
            {
                continue;
            }

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

    private bool CanAutoCastNow() => controller == null || controller.IsReady;

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

    public SkillBuffProfile GetBuffProfile(SkillType type)
    {
        return runtimes.TryGetValue(type, out SkillRuntime runtime) ? runtime.BuffProfile : null;
    }

    public bool TryGetRuntime(SkillType type, out SkillRuntime runtime) => runtimes.TryGetValue(type, out runtime);

    public bool IsSkillUnlocked(SkillType type) =>
        runtimes.TryGetValue(type, out SkillRuntime runtime) && runtime.IsUnlocked;

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

    public void UnlockSkill(string configId, int level = 1)
    {
        if (!ServiceLocator.TryGet(out ConfigManager configManager) ||
            !configManager.TryGetSkill(configId, out SkillDataSO data))
        {
            Debug.LogWarning($"[SkillManager] 未找到技能配置: {configId}");
            return;
        }

        UnlockSkill(data, level);
    }

    public void UnlockSkill(SkillDataSO data, int level = 1)
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
        }

        runtime.Initialize(data, level, true);
        PersistUnlock(data.ConfigId, runtime.BaseData.Level);
        GameEvents.RaiseSkillLevelUp(this, data.ConfigId, runtime.BaseData.Level);
    }

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

    public bool CanShootNow() => shootController != null && shootController.CanShoot();

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

    public void TriggerShootCast()
    {
        shootController?.ExecuteShoot();
    }

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
