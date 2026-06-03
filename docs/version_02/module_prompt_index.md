# Attack Barbarians v2 模块提示词总览

> 架构设计：`architecture_design.md` | 工作流：`.cursor/rules/work_flow.md` | 需求：`requirements_analysis.md`

## 项目定位
Unity 2D 竖屏无限防守 + Roguelike。核心循环：战斗 → 击杀 → 经验 → 升级三选一 Buff → 更高波次 → 局末结算 → 局外永久成长。

## v2 架构分层速查

| 层级 | 模块 | Prompt |
|------|------|--------|
| **L0 Foundation** | Core, Event, Pool, Config, Save, GameState | 见下方 L0 清单 |
| **L1 Combat** | Player, Enemy, Damage, Projectile, Collision, AutoAttack, Skill, Buff | 见下方 L1 清单 |
| **L2 Content** | Wave, Boss, SpecialEnemy, Map, GameplayEvents | 见下方 L2 清单 |
| **L3 Progression** | Upgrade, Talent, Equipment | 见下方 L3 清单 |
| **L4 Presentation** | UI, Audio | 见下方 L4 清单 |
| **L5 Meta** | Shop, Achievement, Daily, Gacha, Stamina | 见下方 L5 清单 |

## 当前代码基础（v2 审计）

| 区域 | 状态 | v2 要求 |
|------|------|---------|
| `Core/` | Bootstrap + GameState 完整 | 保持；systems 顺序已对齐 |
| `Player/` | Controller 驱动状态机 | 属性经 PlayerRuntimeStats 单一路径 |
| `Enemy/` | SpawnerManager 新 + GenerateManager 旧 | 仅扩展 SpawnerManager |
| `SkillSystem/` | 射击为主，7 技能框架 | 每技能 ISkillEffect 独立实现 |
| `Upgrade/` | 三选一 + 4 循环框架 | 验证并补 UI |
| `Talent/Equipment/` | 功能可用 | 使用 PlayerSceneAccess |
| `Utils/PlayerSceneAccess` | **v2 新增** | 全项目 Player 查找入口 |

## 模块依赖规则（全局）

```
允许：L(n) → L(0..n-1) 的接口 / Event / SO
禁止：L(n) → L(n+1..) 直接类引用
禁止：Damage/Pool/Config → UI/Shop
禁止：FindObjectOfType<Player*>（用 PlayerSceneAccess）
禁止：扩展 Legacy（EnemyGenerateManager, PlayerCombat, EnemyCombatManager）
```

## Core Framework 场景挂载
详见 `core_framework_scene_setup.md`。

```
GameSystems [GameBootstrapper, ConfigManager, SaveManager, GameManager, GameFlowManager, ...]
├── PoolRoot [PoolManager]
Player [Player, PlayerController, PlayerSkillManager, SkillManager, BuffManager, AutoAttackController]
```

## 模块提示词文件清单

### L0 Foundation
| 文件 | 模块 |
|------|------|
| `prompt_core_framework.md` | Bootstrap, ServiceLocator, IGameSystem |
| `prompt_singleton_framework.md` | MonoSingleton, SingletonHost |
| `prompt_event_system.md` | EventBus, GameEvents |
| `prompt_object_pool_system.md` | PoolManager |
| `prompt_config_system.md` | ConfigManager, ConfigDatabaseSO |
| `prompt_game_state_system.md` | GameManager, GameFlowManager |
| `prompt_save_system.md` | SaveManager |
| `prompt_input_system.md` | 输入系统 |

### L1 Combat
| 文件 | 模块 |
|------|------|
| `prompt_player_system.md` | Player, PlayerController, PlayerRuntimeStats |
| `prompt_enemy_system.md` | Enemy, EnemySpawnerManager |
| `prompt_damage_system.md` | DamageSystem, DamagePipeline |
| `prompt_projectile_system.md` | ProjectileManager |
| `prompt_auto_attack_system.md` | AutoAttackController |
| `prompt_collision_system.md` | CollisionManager, CollisionQuery |
| `prompt_skill_system.md` | SkillManager, ISkillEffect |
| `prompt_buff_system.md` | BuffManager, SkillBuffProfile |
| `prompt_weapon_system.md` | ShootSkillController |

### L2 Content
| 文件 | 模块 |
|------|------|
| `prompt_wave_system.md` | WaveManager |
| `prompt_boss_system.md` | BossController |
| `special_enemy_system.md` | SpecialEnemy, IEnemyAbility |
| `more_skills_system.md` | 7 元素技能 |
| `prompt_content_map_system.md` | MapManager |
| `gameplay_events_system.md` | GameplayEventManager |

### L3 Progression
| 文件 | 模块 |
|------|------|
| `prompt_upgrade_random_reward_system.md` | UpgradeManager, RandomRewardManager |
| `prompt_talent_equipment_system.md` | TalentManager, EquipmentManager |

### L4 Presentation
| 文件 | 模块 |
|------|------|
| `prompt_ui_system.md` | UI Presenter 模式 |
| `ui_architecture_workflow.md` | UI 分层、通用组件、搭建流程 |
| `main_scene_ui_architecture_refactor.md` | MainScene 场景层级与重构说明 |
| `prompt_main_scene_ui.md` | MainScene 主页面 UI |
| `prompt_ui_tech_wasteland_visual_design.md` | 科技废土视觉（推荐） |
| `prompt_ui_xianxia_visual_design.md` | 仙侠视觉（备选） |
| `ui_mobile_adaptation_design.md` | 移动端 UI 适配架构与原理 |
| `prompt_audio_system.md` | AudioManager |

### L5 Meta
| 文件 | 模块 |
|------|------|
| `prompt_economy_shop_system.md` | 商店/背包 |
| `prompt_achievement_daily_reward_system.md` | 成就/签到/抽奖/体力 |

### 参考文档（非 Prompt）
| 文件 | 用途 |
|------|------|
| `architecture_design.md` | 总架构 |
| `project_analysis.md` | 代码审计 |
| `requirements_analysis.md` | 需求矩阵 |
| `work_plan.md` | 里程碑 |
| `damage_system.md` / `projectile_system.md` 等 | 模块总结 |
| `event_system_catalog.md` | Event Key 目录 |
| `AI_上下文管理_rules.md` | AI 上下文规范 |

## 通用提示词使用流程

1. 读 `module_prompt_index.md`（本文件）和目标 `prompt_*.md`
2. 读 `.cursor/rules/` 全套规则 + `architecture_design.md`
3. 确认本模块所属层级与允许依赖
4. 读目标模块现有脚本
5. 输出：模块依赖 · 文件结构 · 数据流 · 事件流 · 扩展性 · 性能
6. 只实现当前模块
7. 交付：挂载说明 · 测试步骤 · Legacy 边界 · 未完成项

## 代码生成约束

- C# 结构遵循 `csharp_standards.md`
- 高频对象必须对象池
- 跨系统事件必须 Subscribe/Unsubscribe 配对
- 核心数值必须 SO 配置
- 新类加 `///` 注释：是否挂载、推荐物体、获取方式
- Player 查找用 `PlayerSceneAccess`
- 属性修改走 `PlayerRuntimeStats` → `RefreshEntityStats`
