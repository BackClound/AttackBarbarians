# Attack Barbarians 开发工作流（v2）

> 架构详情：`docs/version_02/architecture_design.md`  
> 模块 Prompt 索引：`docs/version_02/module_prompt_index.md`

---

## 开发原则

1. **分层单向依赖**：L0 Foundation → L1 Combat → L2 Content → L3 Progression → L4 Presentation → L5 Meta
2. **Manager + Controller + Config**：Manager 调度，Controller 行为，SO 配置
3. **事件优先**：跨模块走 `GameEvents`，禁止反向引用高层 Manager
4. **数据驱动**：平衡参数不进逻辑代码
5. **对象池强制**：敌人/子弹/特效/飘字
6. **迁移渐进**：Legacy 先 Obsolete + 开关，验证后删除

---

## AI 开发每次必做（生成代码前）

1. 读 `.cursor/rules/project_rules.md`、`architecture_rules.md`、`csharp_standards.md`、`game_rules.md`
2. 读 `docs/version_02/module_prompt_index.md` 中**当前模块** prompt
3. 读 `docs/version_02/architecture_design.md` 确认层级与依赖
4. 输出分析：**模块依赖 · 文件结构 · 数据流 · 事件流 · 扩展性 · 性能**
5. 只实现当前模块，不顺手重构其他模块
6. 交付：挂载说明 · 测试步骤 · 未完成项

---

## 六阶段开发路线

### 阶段 1：Foundation（L0）— 基础框架

| 模块 | 关键类 | Prompt |
|------|--------|--------|
| Bootstrap | GameBootstrapper, ServiceLocator | `prompt_core_framework.md` |
| Singleton | MonoSingleton, SingletonHost | `prompt_singleton_framework.md` |
| Event | EventBus, GameEvents | `prompt_event_system.md` |
| Pool | PoolManager, ObjectPool | `prompt_object_pool_system.md` |
| Config | ConfigManager, ConfigDatabaseSO | `prompt_config_system.md` |
| GameState | GameManager, GameStateMachine, GameFlowManager | `prompt_game_state_system.md` |
| Save | SaveManager | `prompt_save_system.md` |
| Input | InputReader（待建） | `prompt_input_system.md` |

**场景挂载：**
```
GameSystems [GameBootstrapper, ConfigManager, SaveManager, GameManager, GameFlowManager, ...]
└── PoolRoot [PoolManager]
```
详见 `docs/version_02/core_framework_scene_setup.md`

**验收：** Bootstrap 后 ServiceLocator 全就绪；暂停/恢复/GameOver 状态转换正确。

---

### 阶段 2：Combat Core（L1）— 战斗核心

| 模块 | 关键类 | Prompt |
|------|--------|--------|
| Player | Player, PlayerController, PlayerRuntimeStats | `prompt_player_system.md` |
| Enemy | EnemySpawnerManager, Enemy, EnemyCombatBridge | `prompt_enemy_system.md` |
| Damage | DamageSystem, DamagePipeline | `prompt_damage_system.md` |
| Projectile | ProjectileManager | `prompt_projectile_system.md` |
| AutoAttack | AutoAttackController | `prompt_auto_attack_system.md` |
| Collision | CollisionManager, CollisionQuery | `prompt_collision_system.md` |
| Wave | WaveManager | `prompt_wave_system.md` |

**依赖：** 仅 L0（Event, no direct UI）

**数据流：**
```
WaveManager → EnemySpawner → Enemy
PlayerController → AutoAttack/Skill → Projectile → DamagePipeline → GameEvents
PlayerRuntimeStats → ConfigStatBridge → Entity_Stats
```

**验收：** 3 波战斗闭环；伤害/击杀事件正确；无 legacy spawner 激活。

---

### 阶段 3：Progression（L1/L3）— 成长系统

| 模块 | 关键类 | Prompt |
|------|--------|--------|
| Skill | SkillManager, ISkillEffect | `prompt_skill_system.md` |
| Buff | BuffManager, SkillBuffProfile | `prompt_buff_system.md` |
| Weapon | ShootSkillController | `prompt_weapon_system.md` |
| Upgrade | UpgradeManager, RandomRewardManager | `prompt_upgrade_random_reward_system.md` |
| Talent | TalentManager | `prompt_talent_equipment_system.md` |
| Equipment | EquipmentManager | `prompt_talent_equipment_system.md` |

**关键规则：**
- 属性唯一真相源：`PlayerRuntimeStats` → `ConfigStatBridge` → `Entity_Stats`
- Buff 路由：`BuffManager` 分属性/技能，不重复应用
- 4 循环：3 属性 Buff + 1 技能 Buff
- Player 查找：统一 `PlayerSceneAccess`

**验收：** 三选一 → Buff 生效 → 属性同步 → 存档持久化。

---

### 阶段 4：Content（L2）— 内容系统

| 模块 | Prompt |
|------|--------|
| Boss | `prompt_boss_system.md` |
| SpecialEnemy | `special_enemy_system.md` |
| More Skills | `more_skills_system.md` |
| Map/Events | `prompt_content_map_system.md` |
| GameplayEvents | `gameplay_events_system.md` |

**验收：** Boss 定时出现；至少 1 特殊 Enemy 能力；1 张可玩地图。

---

### 阶段 5：Presentation & Meta（L4/L5）— 外围系统

| 模块 | Prompt |
|------|--------|
| UI | `prompt_ui_system.md` |
| UI 视觉 | `prompt_ui_tech_wasteland_visual_design.md` |
| Audio | `prompt_audio_system.md` |
| Economy/Shop | `prompt_economy_shop_system.md` |
| Achievement/Daily | `prompt_achievement_daily_reward_system.md` |

**规则：** UI 只订阅 Event，不驱动战斗逻辑。

**验收：** HUD 全套；结算界面；商店/签到各 1 条 happy path。

---

### 阶段 6：Optimization — 优化

| 任务 | Prompt |
|------|--------|
| 性能/GC/内存/移动端 | `prompt_performance_optimization.md` |
| asmdef 分阶段引入 | `architecture_design.md` §6.1 |
| Legacy 清理 | `project_analysis.md` §3 |
| Namespace 迁移 | 新代码 `AttackBarbarians.{Layer}.{Module}` |

**验收：** 50 敌人同屏 ≥55fps；零 Obsolete 引用。

---

## 模块依赖速查

```
允许：UpgradeManager → BuffManager → PlayerController
允许：WaveManager → EnemySpawnerManager → ConfigManager
允许：UI Presenter → GameEvents（Subscribe only）

禁止：DamageSystem → UpgradeManager
禁止：PoolManager → UI
禁止：ConfigManager → SkillManager（运行时）
禁止：L0 → L3+ 直接类引用
```

---

## 重构检查清单（v2 技术债）

- [ ] 删除 `EnemyGenerateManager` 场景引用
- [ ] 删除 `PlayerCombat` / `EnemyCombatManager`
- [ ] `DamagePipeline.ApplyLegacy` 仅测试环境
- [ ] 全部 `FindObjectOfType<Player*>` → `PlayerSceneAccess`
- [ ] AutoAttack 不独立发弹，驱动 SkillManager
- [ ] asmdef 分阶段 Apply 至 Phase 10（工具已就绪，见 `docs/version_02/asmdef_migration.md`）
- [ ] Wall 代理层文档化（敌人伤害 → Player 血量）

---

## 文档体系

| 路径 | 用途 |
|------|------|
| `docs/version_02/architecture_design.md` | 总架构 |
| `docs/version_02/project_analysis.md` | 代码分析 |
| `docs/version_02/requirements_analysis.md` | 需求矩阵 |
| `docs/version_02/work_plan.md` | 里程碑计划 |
| `docs/version_02/module_prompt_index.md` | Prompt 索引 |
| `docs/version_02/prompt_*.md` | 各模块 AI 开发 prompt |
| `.cursor/rules/*.md` | Cursor 全局规则 |
