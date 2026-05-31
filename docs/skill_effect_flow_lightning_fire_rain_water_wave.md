# Lightning / FireRain / WaterWave 技能生效流程

本文档分析项目中闪电、火雨、水浪技能从解锁触发、自动释放、效果生效、造成伤害到 VFX 播放的完整代码链路。

说明：需求中提到的 `LightingSkill` 在当前工程代码里实际命名为 `LightningSkillEffect`，配置 ID 为 `skill.lightning`，技能类型为 `SkillType.Lightning`。项目中没有名为 `LightingSkill` 或 `LightingSkillEffect` 的类。

## 相关代码入口

- 技能类型枚举：`Assets/Scripts/SkillSystem/Data/SkillType.cs`
- 技能配置资产：`Assets/Resources/Config/Skill/SkillData_Lightning.asset`、`SkillData_FireRain.asset`、`SkillData_WaterWave.asset`
- 技能运行时管理：`Assets/Scripts/SkillSystem/Core/SkillManager.cs`
- 技能上下文：`Assets/Scripts/SkillSystem/Core/SkillContext.cs`
- 技能效果接口：`Assets/Scripts/SkillSystem/Core/ISkillEffect.cs`
- 具体技能效果：`Assets/Scripts/SkillSystem/Effects/SkillEffectImplementations.cs`
- 技能 VFX：`Assets/Scripts/SkillSystem/Vfx/SkillCastVfxPlayer.cs`
- 伤害入口：`Assets/Scripts/Damage/DamagePipeline.cs`
- 伤害结算：`Assets/Scripts/Damage/DamageSystem.cs`
- 敌人血量：`Assets/Scripts/Enemy/Enemy_Health.cs`
- 敌人状态：`Assets/Scripts/Enemy/EnemyStatusController.cs`
- 元进度解锁：`Assets/Scripts/SkillSystem/SkillUnlockService.cs`
- 局内升级应用：`Assets/Scripts/Upgrade/Core/UpgradeApplicator.cs`

## 总体调用链

```text
元进度 / 局内升级 / Debug 解锁
  -> SkillManager.UnlockSkill(configId, level)
  -> ConfigManager.TryGetSkill(configId, out SkillDataSO)
  -> SkillRuntime.Initialize(data, level, unlocked: true)
  -> 加入 runtimes 与 autoCastOrder

每帧 SkillManager.Update()
  -> 检查 enableAutoCast、SkillContext、PlayerController.IsReady
  -> 遍历 autoCastOrder
  -> 检查 runtime.IsUnlocked
  -> 检查 runtime.IsCooldownReady
  -> 检查 runtime.Config.AutoCast
  -> ISkillEffect.TryAutoCast(context, runtime)
  -> 成功后 runtime.StartCooldown()

ISkillEffect.TryAutoCast()
  -> SkillContext 查目标 / 查范围敌人
  -> SkillContext.BuildDamageInfo()
  -> DamagePipeline.Apply()
  -> DamageSystem.ApplyDamage()
  -> Entity_Health.ApplyResolvedDamage()
  -> Enemy_Health.ReduceHp()
  -> 击杀时 Enemy_Health.Die()
  -> GameEvents.RaiseDamageApplied / RaiseEnemyKilled
```

## 技能配置

三个技能都来自 `SkillDataSO`，关键字段在 `Assets/Scripts/Config/Data/SkillDataSO.cs` 中定义：

```csharp
[SerializeField] private SkillType skillType;
[SerializeField] private GameObject skillPrefab;
[SerializeField] private ElementType elementType = ElementType.Physical;
[SerializeField] private float baseDamage = 12f;
[SerializeField] private float baseCooldown = 4f;
[SerializeField] private float areaRadius = 4f;
[SerializeField] private bool autoCast = true;
```

当前资产值如下。

- `SkillData_Lightning.asset`
  - `configId: skill.lightning`
  - `skillType: 3`，对应 `SkillType.Lightning`
  - `elementType: 4`，对应闪电元素
  - `baseDamage: 14`
  - `baseCooldown: 2.5`
  - `areaRadius: 6`
  - `autoCast: 1`
  - `skillPrefab: {fileID: 0}`，未配置 Prefab

- `SkillData_FireRain.asset`
  - `configId: skill.fire_rain`
  - `skillType: 1`，对应 `SkillType.FireRain`
  - `elementType: 2`，对应火元素
  - `baseDamage: 10`
  - `baseCooldown: 4`
  - `areaRadius: 4`
  - `autoCast: 1`
  - `skillPrefab: {fileID: 0}`，未配置 Prefab

- `SkillData_WaterWave.asset`
  - `configId: skill.water_wave`
  - `skillType: 5`，对应 `SkillType.WaterWave`
  - `elementType: 1`，对应 `ElementType.Physical`；当前项目没有单独的 Water 元素枚举
  - `baseDamage: 12`
  - `baseCooldown: 3`
  - `areaRadius: 3`
  - `autoCast: 1`
  - `skillPrefab: {fileID: 0}`，未配置 Prefab

`SkillDataSO.SkillPrefab` 当前没有被 `LightningSkillEffect`、`FireRainSkillEffect`、`WaterWaveSkillEffect` 使用；这三个技能的生效主要由代码逻辑直接完成。

## 解锁与触发逻辑

### 元进度解锁

`SkillUnlockService` 根据累计游玩时间和存档决定技能是否可用。默认阈值写在 `FallbackRules` 中：

```csharp
new() { SkillId = GameConstants.ConfigIds.SkillLightning, RequiredSeconds = 600, UnlockedByDefault = false },
new() { SkillId = GameConstants.ConfigIds.SkillFireRain, RequiredSeconds = 3600, UnlockedByDefault = false },
new() { SkillId = GameConstants.ConfigIds.SkillWaterWave, RequiredSeconds = 7200, UnlockedByDefault = false },
```

开局时有两条同步路径：

```csharp
// SkillManager.Start()
unlockService.ApplyUnlocksToPlayerSkillManager();

// SkillUnlockService.OnGameStarted()
RefreshMetaUnlocks();
ApplyUnlocksToPlayerSkillManager();
```

`ApplyUnlocksToPlayerSkillManager()` 内部会解析 `PlayerSkillManager.SkillManager`，然后调用：

```csharp
manager.UnlockSkill(skillId, level);
```

### 局内升级触发

局内三选一或奖励系统走 `UpgradeManager.TryApplyChoice()`：

```text
UpgradeManager.TryApplyChoice(option, triggerSource)
  -> TryApplyEffect(option)
  -> UpgradeApplicator.TryApply(option, playerSkillManager, saveManager)
```

如果升级项是解锁技能：

```csharp
case UpgradeEffectType.SkillUnlock:
    return ApplySkillUnlock(option, skillManager);
```

最终调用：

```csharp
skillManager.SkillManager.UnlockSkill(option.SkillConfigId, 1);
```

如果升级项是技能 Buff：

```csharp
case UpgradeEffectType.SkillBuff:
case UpgradeEffectType.WeaponEnhance:
    return ApplySkillBuff(option, skillManager);
```

最终调用：

```csharp
skillManager.ApplySkillBuff(option.SkillBuffKind, option.SkillBuffTier);
```

### SkillManager 注册与自动释放

`SkillManager.Awake()` 会注册所有内置技能效果：

```csharp
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
```

`SkillEffectFactory.Create()` 将枚举映射到具体效果类：

```csharp
case SkillType.Lightning:
    return new LightningSkillEffect();
case SkillType.FireRain:
    return new FireRainSkillEffect();
case SkillType.WaterWave:
    return new WaterWaveSkillEffect();
```

`UnlockSkill()` 会读取配置并初始化运行时：

```csharp
runtime.Initialize(data, level, true);
PersistUnlock(data.ConfigId, runtime.BaseData.Level);
GameEvents.RaiseSkillLevelUp(this, data.ConfigId, runtime.BaseData.Level);
```

`SkillRuntime.Initialize()` 的关键行为：

```csharp
Config = config;
IsUnlocked = unlocked && config != null;
buffProfile.Reset();
baseData.Initialize(config, level);
CooldownSeconds = config.BaseCooldown;
LastCastTime = Time.time - CooldownSeconds;
```

`LastCastTime = Time.time - CooldownSeconds` 表示解锁后第一次自动施法可以立即通过冷却检查。

每帧自动释放在 `SkillManager.Update()` 中执行：

```csharp
for (int i = 0; i < autoCastOrder.Count; i++)
{
    SkillType type = autoCastOrder[i];

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
```

## 目标查询与伤害构造

三个技能都依赖 `SkillContext` 获取目标、范围敌人并构造伤害。

主目标查询：

```csharp
public bool TryGetPrimaryTarget(out Enemy enemy)
{
    enemy = null;
    if (Controller != null && Controller.IsReady)
    {
        enemy = Controller.GetPrimaryTarget();
        if (enemy != null && enemy.enemy_Health != null && enemy.enemy_Health.CanBeDamage())
        {
            return true;
        }
    }

    return TryCopyTargets(targetBuffer) && targetBuffer.Count > 0 && (enemy = targetBuffer[0]) != null;
}
```

范围敌人查询：

```csharp
public int QueryEnemiesInCircle(Vector2 center, float radius, List<Enemy> results)
{
    results?.Clear();
    LayerMask layers = Physics2D.DefaultRaycastLayers;
    if (ServiceLocator.TryGet(out CollisionManager collisionManager))
    {
        layers = collisionManager.GetPlayerEnemyScanLayers(0);
    }

    int count = CollisionQuery.OverlapCircleNonAlloc(center, radius, layers, OverlapScratch);
    for (int i = 0; i < count; i++)
    {
        if (!CollisionQuery.TryResolveEnemy(OverlapScratch[i], out Enemy enemy))
        {
            continue;
        }

        if (enemy.enemy_Health == null || !enemy.enemy_Health.CanBeDamage())
        {
            continue;
        }

        results?.Add(enemy);
    }
}
```

伤害构造：

```csharp
public DamageInfo BuildDamageInfo(SkillRuntime runtime, GameObject target, float skillMultiplier = 1f)
{
    SkillDataSO config = runtime.Config;
    float baseDamage = GetBaseDamage(config) * runtime.GetDamageMultiplier() * skillMultiplier;
    ElementType element = config != null ? config.ElementType : ElementType.None;
    string skillId = config != null ? config.ConfigId : string.Empty;
    return DamageInfo.Create(Player, target, baseDamage, 1f, skillId, element);
}
```

注意：`GetBaseDamage()` 优先取玩家当前攻击力，而不是配置里的 `baseDamage`：

```csharp
float fromConfig = config != null ? config.BaseDamage : 10f;
if (Player != null && Player.player_Health != null && Player.player_Health.entity_Stats != null)
{
    fromConfig = Player.player_Health.entity_Stats.GetBaseAttackDamage();
}
return fromConfig;
```

因此这三个技能实际伤害基准是玩家攻击力，再叠加技能内部传入的 `skillMultiplier`、技能 Buff 的 `DamageMultiplier`、元素加成、护甲减免和暴击。

## 闪电 LightningSkillEffect

### 触发条件

`LightningSkillEffect.TryAutoCast()` 首先要求：

- `context != null`
- `runtime.Config != null`
- `runtime.Config.AutoCast == true`
- 能通过 `context.TryGetPrimaryTarget(out Enemy primary)` 找到可受伤主目标

### 生效逻辑

核心代码：

```csharp
SkillBuffProfile buff = runtime.BuffProfile;
int bolts = Mathf.Max(1, buff.LightningBolts);
int chainLen = Mathf.Max(1, buff.ChainTargets);

for (int b = 0; b < bolts; b++)
{
    Enemy start = b == 0 ? primary : primary;
    List<Enemy> chain = context.GetChainTargets(start, chainLen, runtime.Config.AreaRadius * 2f);
    Vector2 from = context.CastOrigin != null ? context.CastOrigin.position : context.Player.transform.position;
    for (int i = 0; i < chain.Count; i++)
    {
        Enemy target = chain[i];
        if (target == null)
        {
            continue;
        }

        Vector2 to = target.transform.position;
        SkillCastVfxPlayer.PlayLightningSegment(from, to, chainLen);
        DamageInfo info = context.BuildDamageInfo(runtime, target.gameObject);
        DamagePipeline.Apply(info);

        EnemyStatusController status = target.GetComponent<EnemyStatusController>();
        if (buff.LightningStun && status != null)
        {
            status.ApplyStun(buff.LightningStunDuration);
        }

        if (i < chain.Count - 1)
        {
            from = to;
        }
        else if (buff.LightningEndExplosion)
        {
            context.QueryEnemiesInCircle(to, buff.LightningExplosionRadius, explosionScratch);
            for (int e = 0; e < explosionScratch.Count; e++)
            {
                DamagePipeline.Apply(context.BuildDamageInfo(runtime, explosionScratch[e].gameObject, 0.6f));
            }
        }
    }
}

context.NotifyCast(runtime);
return true;
```

链式目标来自 `SkillContext.GetChainTargets()`：

```text
起点加入 chainBuffer
  -> 从当前战斗目标列表中寻找未使用且距离 cursor 最近的敌人
  -> 距离必须小于 maxLinkDistance
  -> 加入链后 cursor 移动到该敌人位置
  -> 直到达到 maxCount 或找不到下一个目标
```

### 造成伤害流程

闪电每命中链上一个敌人，调用：

```csharp
DamagePipeline.Apply(context.BuildDamageInfo(runtime, target.gameObject));
```

如果开启末端爆炸，链最后一个目标位置会再做一次范围查询：

```csharp
context.QueryEnemiesInCircle(to, buff.LightningExplosionRadius, explosionScratch);
DamagePipeline.Apply(context.BuildDamageInfo(runtime, explosionScratch[e].gameObject, 0.6f));
```

末端爆炸使用 `0.6f` 技能倍率。

### 控制效果

如果 `buff.LightningStun == true`，命中目标会麻痹：

```csharp
status.ApplyStun(buff.LightningStunDuration);
```

`EnemyStatusController.ApplyStun()` 会延长 `stunUntil`：

```csharp
stunUntil = Mathf.Max(stunUntil, Time.time + durationSeconds);
```

### VFX Effect 生效流程

闪电是三个技能里唯一有明确 VFX 调用的技能：

```csharp
SkillCastVfxPlayer.PlayLightningSegment(from, to, chainLen);
```

`SkillCastVfxPlayer.PlayLightningSegment()`：

```csharp
public static void PlayLightningSegment(Vector2 from, Vector2 to, int buffTier)
{
    Color color = ResolveLightningColor(buffTier);
    SkillLightningLine line = RentLine();
    line.Play(from, to, color, DefaultLineDuration);
}
```

VFX 细节：

- 使用静态池 `LinePool`，最多 `MaxActiveLines = 24` 条。
- 没有可复用线段时创建 `GameObject("SkillLightningVfx")`。
- 对宿主调用 `Object.DontDestroyOnLoad(host)`。
- 添加内部组件 `SkillLightningLine`。
- `SkillLightningLine.EnsureRenderer()` 创建 `LineRenderer`。
- 材质为 `Shader.Find("Sprites/Default")`。
- 线宽 `widthMultiplier = 0.08f`。
- 排序 `sortingOrder = 50`。
- 持续时间 `DefaultLineDuration = 0.12f`。
- `buffTier` 越高，颜色从蓝色向紫色插值。

核心播放代码：

```csharp
lineRenderer.startColor = color;
lineRenderer.endColor = color;
lineRenderer.positionCount = 2;
lineRenderer.SetPosition(0, from);
lineRenderer.SetPosition(1, to);
lineRenderer.enabled = true;
remaining = duration;
```

`Update()` 中倒计时结束后关闭 `LineRenderer.enabled`，不会销毁对象。

## 火雨 FireRainSkillEffect

### 触发条件

`FireRainSkillEffect.TryAutoCast()` 首先要求：

- `context != null`
- `runtime.Config != null`
- `runtime.Config.AutoCast == true`
- `context.TryCopyTargets(scratch)` 能复制到至少一个当前战斗目标

### 生效逻辑

核心代码：

```csharp
SkillBuffProfile buff = runtime.BuffProfile;
float radius = runtime.Config.AreaRadius * buff.FireRainRadiusScale;
int ticks = 1 + Mathf.RoundToInt(buff.FireRainDurationScale * 3f);
Enemy anchor = scratch[Random.Range(0, scratch.Count)];
Vector2 center = anchor.transform.position;

for (int t = 0; t < ticks; t++)
{
    context.QueryEnemiesInCircle(center, radius, scratch);
    for (int i = 0; i < scratch.Count; i++)
    {
        ApplyFireHit(context, runtime, scratch[i], buff, chainScratch);
    }
}

context.NotifyCast(runtime);
return true;
```

火雨不是持续协程或持续区域对象，而是在一次 `TryAutoCast()` 中同步执行多次 Tick：

```text
从当前战斗目标列表中随机选一个 anchor
  -> center = anchor.transform.position
  -> radius = 配置 areaRadius * FireRainRadiusScale
  -> ticks = 1 + RoundToInt(FireRainDurationScale * 3)
  -> 每个 tick 都查询 center 范围内敌人
  -> 对每个敌人执行 ApplyFireHit()
```

默认 `FireRainDurationScale = 1`，所以默认 `ticks = 4`。

### 造成伤害流程

`ApplyFireHit()`：

```csharp
DamageResult result = DamagePipeline.Apply(context.BuildDamageInfo(runtime, enemy.gameObject, 0.85f));
if (result.IsKill && buff.FireRainChainOnKill > 0 && chainBuffer != null)
{
    chainBuffer.Clear();
    context.QueryEnemiesInCircle(enemy.transform.position, runtime.Config.AreaRadius, chainBuffer);
    int chained = 0;
    for (int i = 0; i < chainBuffer.Count && chained < buff.FireRainChainOnKill; i++)
    {
        if (chainBuffer[i] == enemy)
        {
            continue;
        }

        DamagePipeline.Apply(context.BuildDamageInfo(runtime, chainBuffer[i].gameObject, 0.7f));
        chained++;
    }
}
```

火雨主伤害使用 `0.85f` 技能倍率。若本次伤害击杀敌人，并且 `FireRainChainOnKill > 0`，则以死亡敌人位置为圆心、`runtime.Config.AreaRadius` 为半径查询附近敌人，最多连锁 `FireRainChainOnKill` 个目标，每个连锁目标使用 `0.7f` 技能倍率。

### Buff 对生效逻辑的影响

`SkillBuffCatalog` 写入火雨 Buff：

```csharp
case SkillBuffKind.FireRainRadius:
    profile.FireRainRadiusScale += GetPercentTier(RadiusPercentTiers, tier);
    break;
case SkillBuffKind.FireRainDuration:
    profile.FireRainDurationScale += GetPercentTier(RadiusPercentTiers, tier);
    break;
case SkillBuffKind.FireRainChainOnKill:
    profile.FireRainChainOnKill = tier >= 2 ? 3 : 2;
    break;
```

对应影响：

- `FireRainRadius` 增大 `radius`。
- `FireRainDuration` 增大 `ticks`。
- `FireRainChainOnKill` 允许击杀后追加连锁伤害。

### VFX Effect 生效流程

当前火雨没有实际 VFX 播放流程。

代码依据：

- `SkillData_FireRain.asset` 的 `skillPrefab` 为空。
- `FireRainSkillEffect.TryAutoCast()` 没有调用 `SkillCastVfxPlayer`、`CombatEffectSpawner`、`Object.Instantiate`、`ParticleSystem` 或任何 VFX API。
- `SkillDataSO.SkillPrefab` 也没有在火雨逻辑中读取。

因此当前火雨的可见反馈主要来自：

- `DamageSystem.ApplyDamage()` 成功后对敌人发布 `GameEvents.RaiseDamageApplied()`，用于伤害飘字。
- 敌人死亡时 `Enemy_Health.Die()` 触发死亡表现。

如果要补齐火雨 VFX，建议在 `FireRainSkillEffect` 选定 `center` 后增加一个范围/落点 VFX 播放点，例如新增 `SkillCastVfxPlayer.PlayFireRainArea(center, radius, duration)`，或者接入 `SkillDataSO.SkillPrefab` 并使用对象池播放。

## 水浪 WaterWaveSkillEffect

### 触发条件

`WaterWaveSkillEffect.TryAutoCast()` 首先要求：

- `context != null`
- `runtime.Config != null`
- `runtime.Config.AutoCast == true`
- 能通过 `context.TryGetPrimaryTarget(out Enemy primary)` 找到可受伤主目标

### 生效逻辑

核心代码：

```csharp
SkillBuffProfile buff = runtime.BuffProfile;
Vector2 origin = context.CastOrigin != null ? context.CastOrigin.position : context.Player.transform.position;
Vector2 dir = ((Vector2)primary.transform.position - origin).normalized;
int waves = Mathf.Max(1, buff.WaterWaveCount);
float radius = runtime.Config.AreaRadius * buff.WaterWaveSizeScale;
float slowMult = Mathf.Clamp(1f - buff.WaterSlowPercent, 0.1f, 0.95f);

for (int w = 0; w < waves; w++)
{
    float offset = w * 1.2f;
    Vector2 center = origin + dir * (3f + offset);
    context.QueryEnemiesInCircle(center, radius, scratch);
    for (int i = 0; i < scratch.Count; i++)
    {
        Enemy enemy = scratch[i];
        DamagePipeline.Apply(context.BuildDamageInfo(runtime, enemy.gameObject));
        EnemyStatusController status = enemy.GetComponent<EnemyStatusController>();
        status?.ApplySlow(buff.GetWaterSlowDuration(), slowMult);
    }
}

context.NotifyCast(runtime);
return true;
```

水浪会从玩家施法点向主目标方向生成一个或多个圆形命中区域：

```text
origin = castOrigin 或 player.transform.position
dir = primary.position - origin
waves = WaterWaveCount
radius = 配置 areaRadius * WaterWaveSizeScale

每道 wave:
  offset = w * 1.2
  center = origin + dir * (3 + offset)
  查询 center 范围内敌人
  对每个敌人造成伤害
  对每个敌人施加减速
```

默认 `WaterWaveCount = 1`，默认 `WaterSlowPercent = 0.3`，所以 `slowMult = 0.7`，即敌人移动速度变为 70%。

### 造成伤害流程

每个被波浪圆形范围命中的敌人调用：

```csharp
DamagePipeline.Apply(context.BuildDamageInfo(runtime, enemy.gameObject));
```

水浪没有额外传入技能倍率，所以使用默认 `skillMultiplier = 1f`。

### 控制效果

水浪命中后调用：

```csharp
status?.ApplySlow(buff.GetWaterSlowDuration(), slowMult);
```

`GetWaterSlowDuration()`：

```csharp
public const float BaseWaterSlowDuration = 2f;
public float GetWaterSlowDuration() => BaseWaterSlowDuration * WaterSlowDurationScale;
```

`EnemyStatusController.ApplySlow()`：

```csharp
slowUntil = Mathf.Max(slowUntil, Time.time + durationSeconds);
slowMoveMultiplier = Mathf.Clamp(moveMultiplier, 0.05f, 1f);
```

敌人的移动速度读取 `MoveSpeedMultiplier`：

```csharp
return Time.time < slowUntil ? slowMoveMultiplier : 1f;
```

### Buff 对生效逻辑的影响

`SkillBuffCatalog` 写入水浪 Buff：

```csharp
case SkillBuffKind.WaterWaveCount:
    profile.WaterWaveCount = tier >= 2 ? 3 : 2;
    break;
case SkillBuffKind.WaterSlowStrength:
    profile.WaterSlowPercent += GetPercentTier(new[] { 0.3f, 0.5f, 0.8f }, tier);
    profile.WaterSlowPercent = Mathf.Clamp(profile.WaterSlowPercent, 0.1f, 0.9f);
    break;
case SkillBuffKind.WaterSlowDuration:
    profile.WaterSlowDurationScale += GetPercentTier(RadiusPercentTiers, tier);
    break;
case SkillBuffKind.WaterWaveSize:
    profile.WaterWaveSizeScale += GetPercentTier(new[] { 0.3f, 0.5f }, tier);
    break;
```

对应影响：

- `WaterWaveCount` 增加波次数量。
- `WaterSlowStrength` 增加减速百分比，最终转成 `slowMult = 1 - WaterSlowPercent`。
- `WaterSlowDuration` 增加减速持续时间。
- `WaterWaveSize` 增大每道波的命中半径。

### VFX Effect 生效流程

当前水浪没有实际 VFX 播放流程。

代码依据：

- `SkillData_WaterWave.asset` 的 `skillPrefab` 为空。
- `WaterWaveSkillEffect.TryAutoCast()` 没有调用 `SkillCastVfxPlayer`、`CombatEffectSpawner`、`Object.Instantiate`、`ParticleSystem` 或任何 VFX API。
- `SkillDataSO.SkillPrefab` 也没有在水浪逻辑中读取。

因此当前水浪的可见反馈主要来自：

- `DamageSystem.ApplyDamage()` 成功后对敌人发布 `GameEvents.RaiseDamageApplied()`，用于伤害飘字。
- 命中敌人后 `EnemyStatusController.ApplySlow()` 改变敌人移动速度。
- 敌人死亡时 `Enemy_Health.Die()` 触发死亡表现。

如果要补齐水浪 VFX，建议在每个 `center` 位置增加圆形波纹特效，或根据 `origin`、`dir` 和 `waves` 播放一段方向性水浪 Prefab。

## 统一伤害管线

三个技能最终都进入 `DamagePipeline.Apply()`：

```csharp
public static DamageResult Apply(DamageInfo info)
{
    if (info.Target == null)
    {
        return DamageResult.None;
    }

    if (ServiceLocator.TryGet(out DamageSystem damageSystem))
    {
        return damageSystem.ApplyDamage(info);
    }

    GameDebug.LogWarning("[DamagePipeline] DamageSystem 未注册，跳过伤害。请确认 GameBootstrapper 已执行 Bootstrap。");
    return DamageResult.None;
}
```

`DamageSystem.ApplyDamage()`：

```csharp
Entity_Health health = ResolveHealth(info.Target);
if (health == null || !health.CanBeDamage())
{
    return DamageResult.None;
}

DamageResult result = Calculate(info, health);
if (result.FinalDamage <= 0f)
{
    return result;
}

health.ApplyResolvedDamage(result, info);
bool isKill = !health.CanBeDamage();
if (isKill)
{
    result = result.WithKill(true);
}

if (ShouldPublishDamageNumber(info.Target))
{
    GameEvents.RaiseDamageApplied(...);
}

return result;
```

`DamageSystem.Calculate()` 的计算顺序：

```text
基础伤害
  -> 技能倍率
  -> 攻击方元素附加
  -> 防御护甲减免
  -> 元素倍率
  -> 暴击判定与暴击倍率
  -> DamageResult
```

关键公式代码：

```csharp
float damage = info.BaseDamage * info.SkillMultiplier;

if (attackerStats != null)
{
    damage += GetElementBonus(attackerStats, info.ElementType, rules.ElementStatScale);
}

if (!bypassArmor && defenderStats != null)
{
    float armor = defenderStats.GetArmorDefense();
    float armorPen = attackerStats != null && attackerStats.defenseStats != null && attackerStats.defenseStats.armorReduce != null
        ? attackerStats.defenseStats.armorReduce.GetValue()
        : 0f;
    float effectiveArmor = Mathf.Max(0f, armor - armorPen);
    float afterArmor = rules.ApplyArmor(damage, effectiveArmor);
    mitigated = damage - afterArmor;
    damage = afterArmor;
}

damage *= GetElementMultiplier(info.ElementType);

bool isCritical = info.DamageType == DamageType.Critical;
if (!isCritical && (!info.IsDot || rules.AllowDotCritical))
{
    isCritical = RollCritical(attackerStats, rules);
}

if (isCritical && attackerStats != null && attackerStats.offenseStats != null && attackerStats.offenseStats.critPower != null)
{
    float critMult = 1f + attackerStats.offenseStats.critPower.GetValue();
    damage *= critMult;
}
```

敌人扣血在 `Enemy_Health.ApplyResolvedDamage()` 和 `ReduceHp()`：

```csharp
if (result.FinalDamage <= 0f || !CanBeDamage())
{
    return;
}

float finalDamage = result.FinalDamage;
if (TryGetComponent(out EnemyDamageShield shield) && shield.IsActive)
{
    float absorbed = finalDamage - shield.AbsorbDamage(finalDamage);
    mitigated += absorbed;
}

OnBeforeDamageApplied(info, adjusted);
ReduceHp(finalDamage);
```

```csharp
currentHp -= damage;

if (currentHp <= 0 && !isDead)
{
    isDead = true;
    Die();
    lastDamageSource = null;
}
```

死亡事件：

```csharp
GameEvents.RaiseEnemyKilled(enemy, new EnemyEventArgs(
    enemy.gameObject,
    enemy.transform.position,
    lastDamageSource,
    controller != null ? controller.ConfigId : string.Empty,
    experienceReward));
```

## 技能释放通知

三个技能在成功释放后都会调用：

```csharp
context.NotifyCast(runtime);
return true;
```

`NotifyCast()` 内部调用：

```csharp
Controller.NotifySkillCast(runtime.Config.ConfigId);
```

随后 `SkillManager.Update()` 收到 `TryAutoCast()` 返回 `true`，调用：

```csharp
runtime.StartCooldown();
```

`StartCooldown()`：

```csharp
LastCastTime = Time.time;
CastCount++;
```

下一次释放由：

```csharp
Time.time >= LastCastTime + buffProfile.GetEffectiveCooldown(CooldownSeconds)
```

决定。全局冷却缩减会修改 `SkillBuffProfile.CooldownMultiplier`，下限为 `MinCooldownSeconds = 0.15f`。

## 当前 VFX 覆盖范围总结

- 闪电：有 VFX。每段链路调用 `SkillCastVfxPlayer.PlayLightningSegment()`，使用 `LineRenderer` 画一条短暂线段。
- 火雨：无独立 VFX。没有读取 `SkillPrefab`，没有实例化粒子或范围特效。
- 水浪：无独立 VFX。没有读取 `SkillPrefab`，没有实例化粒子或波纹特效。
- 三者命中后的通用反馈：伤害飘字来自 `DamageSystem` 发布的 `GameEvents.RaiseDamageApplied()`；死亡反馈来自敌人死亡流程。

## 建议的 VFX 补齐方向

如果后续要让三个技能都有对应的 `Effect` Prefab 生效，可以统一扩展 `SkillCastVfxPlayer` 或新增 `SkillEffectVfxSpawner`：

```csharp
public static bool TryPlaySkillPrefab(SkillRuntime runtime, Vector3 position, Quaternion rotation, float lifetime = -1f)
{
    GameObject prefab = runtime?.Config != null ? runtime.Config.SkillPrefab : null;
    if (prefab == null)
    {
        return false;
    }

    return CombatEffectSpawner.TrySpawnHitEffect(prefab, position, rotation, lifetime);
}
```

然后在具体技能中接入：

```csharp
// FireRain 选定 center 后
SkillEffectVfxSpawner.TryPlaySkillPrefab(runtime, center, Quaternion.identity, 1f);

// WaterWave 每个 wave center
SkillEffectVfxSpawner.TryPlaySkillPrefab(runtime, center, Quaternion.identity, 0.5f);

// Lightning 可以保留 LineRenderer，也可以给每个 target 播命中特效
SkillEffectVfxSpawner.TryPlaySkillPrefab(runtime, to, Quaternion.identity, 0.2f);
```

注意：当前 `CombatEffectSpawner.TrySpawnHitEffect()` 使用的是 `GameConstants.PoolKeys.CombatVfx`，如果不同技能需要不同 Prefab 池，建议扩展对象池 Key 或让 Prefab 自带 `PooledTimedVfx`。
