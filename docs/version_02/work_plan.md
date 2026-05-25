# Attack Barbarians v2 工作计划

## Phase 0：架构基线（当前 Sprint）✅

| 任务 | 产出 | 状态 |
|------|------|------|
| 架构设计文档 | `architecture_design.md` | ✅ |
| 项目/需求分析 | `project_analysis.md`, `requirements_analysis.md` | ✅ |
| 工作流重写 | `.cursor/rules/work_flow.md` | ✅ |
| v2 Prompt 体系 | `docs/version_02/prompt_*.md` | ✅ |
| PlayerSceneAccess | 统一 Player 查找 | ✅ |
| BuffManager 去 Find | 序列化 + Singleton | ✅ |
| Bootstrap 顺序对齐 | GameBootstrapper | ✅ |

## Phase 1：Foundation 稳固（1-2 周）

| 任务 | 依赖 | 验收 |
|------|------|------|
| 引入 AB.Core asmdef | Phase 0 | Core/Events/Pool 独立编译 |
| Legacy 伤害管线标记与隔离 | Damage | ✅ 仅 DamagePipeline → DamageSystem |
| 禁用 EnemyGenerateManager 场景引用 | Enemy | ✅ 已移除 legacy 刷怪脚本与场景字段 |
| ConfigStatBridge 单一路径文档化 | Player | Buff 后 Entity_Stats 同步测试 |
| core_framework_scene_setup v2 | Bootstrap | Hierarchy 清单 |

## Phase 2：Combat Core 闭环（2-3 周）

| 任务 | Prompt 参考 | 验收 |
|------|-------------|------|
| Wave 30s 超时 + 清场双条件 | `prompt_wave_system.md` | SimulateWave 通过 |
| AutoAttack ↔ SkillManager 统一 |filtered | `prompt_auto_attack_system.md` | 无双射 |
| Projectile 全技能接入池 | `prompt_projectile_system.md` | 无 Instantiate |
| Collision NonAlloc 全覆盖 | `prompt_collision_system.md` | Profiler 无 GC |
| Enemy 状态机统一 Bridge | `prompt_enemy_system.md` | 无 EnemyCombatManager |

## Phase 3：Skill & Buff 完整（3-4 周）

| 任务 | 验收 |
|------|------|
| 7 技能 ISkillEffect 实现 | 各技能 ContextMenu 可触发 |
| SkillBuffKind 全枚举配置化 | SO 驱动，无 hardcode |
| Upgrade 4 循环机制 | 单元测试 12 次选择序列 |
| BuffManager 路由测试 | 属性/技能不重复应用 |

## Phase 4：Content & Boss（2 周）

| 任务 | 验收 |
|------|------|
| Boss 波次定时 + 阶段 | BossHud 正确 |
| SpecialEnemy 5 能力 | 各能力独立 Prefab |
| Map 基础 + GameplayEvent | 1 张可玩地图 |

## Phase 5：UI & Meta（3-4 周）

| 任务 | 验收 |
|------|------|
| HUD 全套（波次/血条/Buff/技能 CD） | Figma 对齐 |
| 升级三选一 UI | 与 RandomRewardManager 联动 |
| 结算界面 + 奖励 | RunRewardSettlement |
| 商店/签到/体力（MVP） | 各 1 条 happy path |

## Phase 6：优化 & 发布（2 周）

| 任务 | 验收 |
|------|------|
| Profiler 基准（50 敌人） | ≥55fps 中端机 |
| Addressables 评估 | 文档决策 |
| Legacy 代码删除 | 零 Obsolete 引用 |
| Namespace 全量迁移 | asmdef 完整 |

## 风险与缓解

| 风险 | 缓解 |
|------|------|
| 场景/Prefab 引用断裂 | 每次只迁移一个职责；保留 Obsolete |
| 双属性不同步 | 强制 RefreshEntityStats 单入口 |
| 技能范围大 | 每技能独立 prompt + 最小闭环 |
| asmdef 循环依赖 | 先 Core，再 Combat，逐层拆 |

## 里程碑

- **M1**（Phase 2 末）：可玩战斗闭环，3 波 + 升级
- **M2**（Phase 3 末）：7 技能 + Buff 循环
- **M3**（Phase 5 末）：MVP 可发布
- **M4**（Phase 6 末）：性能达标 + 代码清理
