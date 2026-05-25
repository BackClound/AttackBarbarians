# Attack Barbarians 项目分析报告（v2）

## 1. 代码规模

| 指标 | 数值 |
|------|------|
| C# 脚本 | 231 个（`Assets/Scripts/`） |
| 程序集定义 | 0（全部 `Assembly-CSharp`） |
| Namespace 使用 | 0（全局命名空间） |
| IGameSystem 实现 | 24 个 |
| ScriptableObject 配置类 | ~16 种 Data SO |

## 2. 已完成模块评估

| 模块 | 完成度 | 质量 | 备注 |
|------|--------|------|------|
| Core Bootstrap | ★★★★☆ | 良好 | 生命周期完整；systems 顺序需对齐 |
| GameState / Flow | ★★★★☆ | 良好 | WaveTransition → Upgrade 闭环可用 |
| Event System | ★★★★☆ | 良好 | GameEvents 门面清晰 |
| Config System | ★★★★☆ | 良好 | ID 索引 + Validator |
| Pool System | ★★★☆☆ | 中等 | 底层 + Manager 双层，部分特效未接入 |
| Save System | ★★★★☆ | 良好 | 版本迁移、自动保存 |
| Player | ★★★★☆ | 良好 | Controller 驱动状态机 |
| Enemy | ★★★★☆ | 良好 | Wave/Pool 驱动 Spawner |
| Damage | ★★★★☆ | 良好 | 统一 DamagePipeline → DamageSystem |
| Skill/Buff | ★★★☆☆ | 中等 | 多层门面，部分技能未实现 |
| Wave | ★★★☆☆ | 中等 | 基础波次可用 |
| Upgrade | ★★★☆☆ | 中等 | 三选一逻辑有，UI 不完整 |
| Talent/Equipment | ★★★☆☆ | 中等 | 功能可用，代码重复 |
| Boss/SpecialEnemy | ★★☆☆☆ | 初版 | 框架有，内容少 |
| UI | ★★☆☆☆ | 初版 | Presenter 模式部分采用 |
| Map/Events | ★★☆☆☆ | 初版 | 调试桥接存在 |
| Economy/Shop | ☆☆☆☆☆ | 未实现 | 仅 Event Key 预留 |

## 3. 架构问题清单

### 3.1 高优先级
1. **属性双真相源**：`Stat.GetFinalValue()` 不叠 modifier；战斗读 `Entity_Stats`，成长写 `PlayerRuntimeStats`。
2. ~~**遗留代码未清理**~~：已移除 `EnemyGenerateManager` / `PlayerCombat` / `EnemyCombatManager`；伤害统一 `DamagePipeline` → `DamageSystem`。
3. **FindObject 散落**：Talent/Equipment/Buff/Experience 等 10+ 处。
4. **无模块编译边界**：231 文件同一程序集。

### 3.2 中优先级
5. **IGameSystem 覆盖不完整**：Player/Skill/Buff/AutoAttack 独立 Update。
6. **TalentManager ≈ EquipmentManager**：~80% 结构重复。
7. **技能入口过多**：PlayerSkillManager → SkillManager → ShootSkillController → AutoAttackController。
8. **Wall 与 Player 双防守模型**：project_rules 写城墙，game_rules 写 Player 下侧防守。

### 3.3 低优先级
9. 中英文注释混用、部分拼写错误。
10. `GameEvents.cs` 内嵌 Payload struct。
11. ConfigStatBridge 与 ConfigValidator modifier 算法需同步。

## 4. 重复设计对照

| 领域 | 实现 A | 实现 B | v2 收敛 |
|------|--------|--------|---------|
| 属性 | Entity_Stats.Stat | StatRuntimeSnapshot | Snapshot 为唯一计算源 |
| Buff | BuffDataSO + BuffManager | SkillBuffKind + SkillBuffProfile | BuffManager 路由，不重复应用 |
| 刷怪 | EnemySpawnerManager | EnemyGenerateManager | 仅保留 SpawnerManager |
| 伤害 | DamageSystem | Entity_Health 直算 | 统一 DamagePipeline |
| 射击 | AutoAttackController | ShootSkillController | AutoAttack 驱动 Skill 节拍 |
| 局外成长 | TalentManager | EquipmentManager | 共享 PlayerSceneAccess + 模式一致 |

## 5. 与需求文档差异

| 需求（game_rules） | 当前实现 | 差距 |
|-------------------|----------|------|
| 7 种元素技能全套 | 射击为主，其余框架 | 需补全 ISkillEffect |
| 每 4 次 Buff 属性/技能交替 | UpgradeManager 有框架 | 需验证计数逻辑 |
| Boss 定时随机 | BossController 存在 | 需与 Wave 联动 |
| 商店/签到/抽奖/体力 | 未实现 | L5 阶段 |
| 排行榜 | 未实现 | L5 阶段 |

## 6. v2 重构范围（本次）

- ✅ 架构文档与 work_flow 重写
- ✅ `PlayerSceneAccess` 统一 Player 查找
- ✅ BuffManager 去除 FindAnyObjectByType
- ✅ GameBootstrapper systems 顺序对齐
- ✅ docs/version_02 prompt 文件
- ✅ asmdef 分阶段（`_Assemblies` + `AssemblyMigrationCatalog` + Editor 菜单；默认 Phase 0，需 Unity 内逐阶段 Apply）
- ✅ Legacy 代码删除（`EnemyGenerateManager` / `PlayerCombat` / `EnemyCombatManager` 已下线；`DamagePipeline.ApplyLegacy` 已移除）
- ⏳ Namespace 迁移（新代码先行）
