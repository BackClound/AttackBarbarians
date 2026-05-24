# Auto Attack System 模块需求提示词

> **版本 v2** | 架构层级见 `architecture_design.md` | 工作流见 `.cursor/rules/work_flow.md`

## v2 架构约束（必读）
- **分层依赖**：本模块所属层级不得反向引用高层模块（见 `architecture_design.md` §2）。
- **属性唯一真相源**：`PlayerRuntimeStats` → `ConfigStatBridge` → `Entity_Stats`；禁止直接修改 Entity_Stats 做 Buff。
- **Player 查找**：统一使用 `PlayerSceneAccess`，禁止 `FindObjectOfType<Player*>`。
- **跨模块通信**：优先 `GameEvents`；Manager 之间通过 `ServiceLocator` 或事件，不直接持有场景实体。
- **对象池**：敌人/子弹/特效/飘字必须走 `PoolManager`。
- **Legacy 禁止扩展**：不新增对 `EnemyGenerateManager`、`PlayerCombat`、`EnemyCombatManager` 的依赖。

# Auto Attack System 模块需求提示词

## 目标
实现玩家自动攻击核心闭环：目标扫描调度、连发/休整、投射物生成与事件发布，作为 Weapon System 落地前的基础武器层。

## 当前基础
- `PlayerController` + `PlayerTargetScanner` 已统一目标策略。
- `ProjectileManager` + `ProjectileController` 已承接子弹生成与伤害。
- `PlayerIdleState` / `PlayerShootState` + `SkillShoot` 仍保留旧连发与 `SkillObject_BulletSpawn` 兼容路径。

## 输出要求
- `AutoAttackDataSO`：连发次数、休整时间、扫描间隔、弹道排布、绑定技能 ID、投射物配置。
- `AutoAttackController`：挂在 Player，负责 `CanAttack`、`ExecuteAttack`、目标列表兼容接口。
- 状态机与 `SkillShoot` 在存在 `AutoAttackController` 时优先走新路径。

## 数据流
`PlayerDataSO → PlayerRuntimeStats → PlayerTargetScanner → AutoAttackController → ProjectileSpawnRequest → ProjectileManager → DamageSystem`

## 验收标准
- 射程内有敌人时 Player 自动进入射击动画并发射投射物。
- 连发耗尽后进入配置化休整，休整结束可再次攻击。
- 无目标时不进入 Shoot 状态；扫描使用间隔调度，避免每帧全量扫描。
- `SkillShoot` 旧路径在未挂载 `AutoAttackController` 时仍可运行。

## 模块依赖边界
- 依赖：Config、Player、Projectile、Damage、GameEvents。
- 不依赖：UI、Buff 具体实现、Upgrade、Shop。
- 不直接 `Instantiate` 子弹；必须经 `ProjectileManager`。

## v2 验收标准补充
- 完成后须验证与 `architecture_design.md` 中本模块的数据流、事件流一致。
- 列出受影响脚本、Prefab/Scene 挂载、ContextMenu 或手动测试步骤。
- 标注仍依赖的 Legacy 代码及后续清理计划。
- 新类推荐 namespace：`AttackBarbarians.{Layer}.{Module}`（迁移期可与全局类并存）。

## v2 模块依赖边界
- 仅依赖 architecture_design.md 中本层及以下层的公共接口、配置 SO、Event Key。
- 禁止从低层模块反向引用 UI、Shop、Upgrade 等 unless 本模块即为该层。
- Event Payload 保持小而稳定；禁止暴露 Manager 内部可变状态。
