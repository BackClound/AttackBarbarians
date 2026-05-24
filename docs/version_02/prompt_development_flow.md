# prompt_development_flow.md

> **版本 v2** | 架构层级见 `architecture_design.md` | 工作流见 `.cursor/rules/work_flow.md`

## v2 架构约束（必读）
- **分层依赖**：本模块所属层级不得反向引用高层模块（见 `architecture_design.md` §2）。
- **属性唯一真相源**：`PlayerRuntimeStats` → `ConfigStatBridge` → `Entity_Stats`；禁止直接修改 Entity_Stats 做 Buff。
- **Player 查找**：统一使用 `PlayerSceneAccess`，禁止 `FindObjectOfType<Player*>`。
- **跨模块通信**：优先 `GameEvents`；Manager 之间通过 `ServiceLocator` 或事件，不直接持有场景实体。
- **对象池**：敌人/子弹/特效/飘字必须走 `PoolManager`。
- **Legacy 禁止扩展**：不新增对 `EnemyGenerateManager`、`PlayerCombat`、`EnemyCombatManager` 的依赖。

推荐开发节奏

推荐：
阶段1
让AI：
搭架构
建目录
写基础系统

阶段2
让AI：
写战斗循环
写技能
写Buff

阶段3
让AI：
扩展内容
优化性能
加UI

阶段4
让AI：
自动生成文档
自动测试
自动重构

补充规范

验收标准
- 每个阶段都必须交付一个可运行、可验证的最小闭环，而不是只新增孤立脚本。
- 每个阶段完成后必须列出场景挂载方式、配置资产要求、手动测试步骤和未完成事项。
- 对高频逻辑、对象生成和跨模块通信必须检查性能、GC、事件订阅生命周期和对象池接入情况。

模块依赖边界
- 第一阶段只建立基础框架、事件、配置、对象池、存档和游戏状态入口，不直接实现战斗细节。
- 第二阶段可以依赖第一阶段公共能力，实现 Player、Enemy、Damage、Projectile、Auto Attack、Collision、Wave 的核心闭环。
- 第三阶段可以依赖战斗事件和运行时属性接口，实现 Skill、Buff、Upgrade、Talent、Equipment，不反向修改底层 Damage/Pool 的内部实现。
- 第四、第五阶段通过事件和配置接入内容、UI、Audio、Shop、Achievement 等外围系统，不让外围系统反向控制战斗核心。
- 第六阶段只做可度量优化，禁止借优化名义改变玩法行为。

旧逻辑兼容与迁移约束
- 当前已有 Player、Enemy、SkillShoot、EnemyGenerateManager、Wall 和 UI 逻辑必须保持可运行。
- 新模块优先采用并行接入、适配器或开关替换，验证通过后再移除旧职责。
- 每次开发只迁移一个明确职责，避免一次性跨阶段重构。
- 删除旧脚本、字段、Prefab 引用或场景对象前必须确认无引用；不确定时保留兼容层并记录后续清理。


## v2 验收标准补充
- 完成后须验证与 `architecture_design.md` 中本模块的数据流、事件流一致。
- 列出受影响脚本、Prefab/Scene 挂载、ContextMenu 或手动测试步骤。
- 标注仍依赖的 Legacy 代码及后续清理计划。
- 新类推荐 namespace：`AttackBarbarians.{Layer}.{Module}`（迁移期可与全局类并存）。

## v2 模块依赖边界
- 仅依赖 architecture_design.md 中本层及以下层的公共接口、配置 SO、Event Key。
- 禁止从低层模块反向引用 UI、Shop、Upgrade 等 unless 本模块即为该层。
- Event Payload 保持小而稳定；禁止暴露 Manager 内部可变状态。
