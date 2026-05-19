# Attack Barbarians 项目架构与模块提示词总览

## 项目定位
Attack Barbarians 是 Unity 2D 竖屏无限防守 + Roguelike 游戏。核心循环为：战斗 -> 击杀敌人 -> 获得经验与资源 -> 波次结算 -> 三选一 Buff/技能强化 -> 进入更高波次。

## 当前代码基础
- `Assets/Scripts/Common` 已实现实体基类、状态机、通用血量、属性、战斗触发和简易敌人生成。
- `Assets/Scripts/Player` 已实现 Player 单例、Idle/Shoot/Dead 状态、血量和基础射击链路。
- `Assets/Scripts/Enemy` 已实现 BatEnemy、移动/攻击/死亡状态、敌人攻击墙体逻辑。
- `Assets/Scripts/SkillSystem` 已实现射击技能、子弹生成器、子弹对象和部分技能等级数据。
- `Assets/Scripts/Stats` 已实现基础属性分组，但 Buff/Modifier 管线尚未形成闭环。
- `Assets/Scripts/Wall` 已实现墙体受击代理，将敌人伤害转发到 Player 血量。
- `Assets/Scripts/UI` 已实现伤害数字显示和简易队列复用。

## 架构优化方向
- 统一采用 `Manager + Controller + Config`：Manager 负责系统调度，Controller 负责对象行为，Config/ScriptableObject 负责数据配置。
- 统一事件流：跨模块通信优先走 `GameEvents` / EventBus，减少 `Player.sInstance`、`WallControlManager.sInstance` 等直接耦合。
- 统一数据驱动：Player、Enemy、Skill、Buff、Wave、Boss、Drop、Audio、UI 文案均通过 ScriptableObject 或配置表驱动。
- 统一对象池：敌人、子弹、特效、伤害数字、掉落物均接入 `PoolManager`，禁止高频 `Instantiate/Destroy`。
- 统一战斗模型：用 `DamageInfo` 携带攻击来源、目标、技能、元素、暴击、DOT、击退等上下文。
- 统一阶段推进：按 `work_flow.md` 分为基础框架、战斗核心、成长系统、内容系统、外围系统、优化阶段。

## Core Framework 场景挂载
- 详见 `docs/core_framework_scene_setup.md`（Manager 挂载位置、Hierarchy、配置资产、验证步骤）。
- 生成 Core 相关代码时必须为每个类补充 `///` 注释，说明是否挂载及推荐 GameObject。

## 模块提示词文件清单
- `prompt_core_framework.md`
- `core_framework_scene_setup.md`
- `prompt_singleton_framework.md`
- `singleton_framework.md`
- `prompt_event_system.md`
- `prompt_object_pool_system.md`
- `prompt_config_system.md`
- `prompt_game_state_system.md`
- `prompt_input_system.md`
- `prompt_player_system.md`
- `prompt_enemy_system.md`
- `prompt_damage_system.md`
- `prompt_projectile_system.md`
- `prompt_skill_system.md`
- `prompt_buff_system.md`
- `prompt_weapon_system.md`
- `prompt_talent_equipment_system.md`
- `prompt_wave_system.md`
- `prompt_boss_system.md`
- `prompt_upgrade_random_reward_system.md`
- `prompt_ui_system.md`
- `prompt_audio_system.md`
- `prompt_save_system.md`
- `prompt_economy_shop_system.md`
- `prompt_achievement_daily_reward_system.md`
- `prompt_content_map_system.md`
- `prompt_performance_optimization.md`

## 通用提示词使用流程
1. 先读取本文件和目标模块提示词。
2. 再读取 `.cursor/rules/project_rules.md`、`.cursor/rules/architecture_rules.md`、`.cursor/rules/csharp_standards.md`、`.cursor/rules/game_rules.md`、`.cursor/rules/work_flow.md`。
3. 读取目标模块相关脚本和已有 `docs` 文档。
4. 输出实现前分析：模块依赖、文件结构、数据流、事件流、扩展性、性能影响。
5. 只实现当前模块，不顺手重构其他模块。
6. 完成后更新该模块的总结、API、数据流、事件流、扩展点和后续任务。

## 代码生成约束
- C# 文件结构遵循 `csharp_standards.md`。
- 高频对象必须接入对象池。
- 高频检测优先使用 NonAlloc API 或可控频率扫描。
- 跨系统事件必须有订阅和取消订阅生命周期。
- 所有核心数值必须可配置，不把平衡参数硬编码在逻辑中。
- 新增系统必须给出最小可运行闭环，而不是只输出伪代码。
