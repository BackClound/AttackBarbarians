# Collision System 模块需求提示词

> **版本 v2** | 架构层级见 `architecture_design.md` | 工作流见 `.cursor/rules/work_flow.md`

## v2 架构约束（必读）
- **分层依赖**：本模块所属层级不得反向引用高层模块（见 `architecture_design.md` §2）。
- **属性唯一真相源**：`PlayerRuntimeStats` → `ConfigStatBridge` → `Entity_Stats`；禁止直接修改 Entity_Stats 做 Buff。
- **Player 查找**：统一使用 `PlayerSceneAccess`，禁止 `FindObjectOfType<Player*>`。
- **跨模块通信**：优先 `GameEvents`；Manager 之间通过 `ServiceLocator` 或事件，不直接持有场景实体。
- **对象池**：敌人/子弹/特效/飘字必须走 `PoolManager`。
- **Legacy 禁止扩展**：不新增对 `EnemyGenerateManager`、`PlayerCombat`、`EnemyCombatManager` 的依赖。

# Collision System 模块需求提示词

## 目标
统一战斗物理查询（目标扫描、墙体探测、投射物命中），NonAlloc 优先，并与 Damage / Auto Attack 解耦。

## 输出要求
- `CollisionDataSO`、`CollisionManager`、`CollisionQuery`、`CollisionProfile`
- 注册到 `GameBootstrapper` / `ServiceLocator`
- 迁移 `PlayerTargetScanner`、`ProjectileController`、`Enemy` 墙体攻击射线

## 验收标准
- 所有高频 Overlap 使用 NonAlloc 或 Manager 共享缓冲
- Player / Enemy 状态机由 Controller 驱动
- 伤害结算仍只经 `DamagePipeline` / `DamageSystem`

## v2 验收标准补充
- 完成后须验证与 `architecture_design.md` 中本模块的数据流、事件流一致。
- 列出受影响脚本、Prefab/Scene 挂载、ContextMenu 或手动测试步骤。
- 标注仍依赖的 Legacy 代码及后续清理计划。
- 新类推荐 namespace：`AttackBarbarians.{Layer}.{Module}`（迁移期可与全局类并存）。

## v2 模块依赖边界
- 仅依赖 architecture_design.md 中本层及以下层的公共接口、配置 SO、Event Key。
- 禁止从低层模块反向引用 UI、Shop、Upgrade 等 unless 本模块即为该层。
- Event Payload 保持小而稳定；禁止暴露 Manager 内部可变状态。
