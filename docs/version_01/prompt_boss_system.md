# Boss System 模块需求提示词

## 目标
实现 Boss System，支持阶段行为、技能释放、特殊机制、Boss 波次、专属 UI、奖励和存档统计。

## 当前基础
Enemy 系统已有普通敌人的状态机基础，但 Boss 尚未独立设计。`project_rules.md` 要求 Boss 支持，`work_flow.md` 将 Boss 放在内容系统阶段。

## 输出要求
- 生成 `BossDataSO`：ID、名称、Prefab、生命、攻击、阶段阈值、技能列表、奖励表。
- 生成 `BossController`：继承或组合 Enemy 基础能力。
- 生成 `BossPhase`：按血量、时间或事件切换阶段。
- 生成 `BossSkillDataSO`：冲锋、召唤、范围攻击、护盾、弹幕等技能配置。
- Boss 出现、阶段切换、死亡均发布事件。

## 实现流程
1. 复用 Enemy 的血量、受击、死亡和对象池基础。
2. Boss 独立处理阶段和技能冷却。
3. WaveManager 在指定波次触发 BossWave。
4. UI 订阅 Boss 事件显示 Boss 血条和阶段提示。
5. Boss 死亡触发特殊奖励和下一阶段内容解锁。

## 数据流
`WaveManager -> BossSpawner -> BossController -> BossPhase -> BossSkill -> DamageSystem/EventBus`

## 验收标准
- Boss 能在指定波次出现并阻止普通波次完成。
- Boss 阶段切换可配置且有 UI/音效反馈。
- Boss 死亡奖励和统计能被 Save/Reward 系统接收。

## 验收标准补充
- 本模块完成后必须形成最小可运行闭环，可以在当前 Unity 场景中通过手动挂载或现有入口验证核心流程。
- 相关配置、运行时数据、事件发布和事件订阅必须有明确入口，异常或缺失配置时要给出可定位的问题表现。
- 高频逻辑不得引入明显 GC 分配；涉及生成、销毁、特效、投射物、敌人或 UI 飘字时必须优先接入对象池。
- 完成实现后必须说明受影响脚本、Prefab/Scene 挂载要求、测试步骤和未迁移的旧逻辑边界。

## 模块依赖边界
- 本模块只能依赖已完成或同阶段明确约定的公共接口、配置数据和事件 Key，不直接依赖后续阶段的具体实现类。
- 跨模块通信优先通过 `EventBus`、`IGameSystem`、`ServiceLocator`、ScriptableObject 配置或明确的 Controller API 完成。
- 禁止从低层模块反向引用高层模块；例如 Damage、Pool、Config 不应依赖 UI、Upgrade、Shop 等外围系统。
- 禁止在核心逻辑中散落 `FindObjectOfType`、硬编码场景路径或直接访问无关单例；必须通过启动流程、序列化引用或服务注册注入依赖。
- 数据结构与事件 Payload 必须保持小而稳定，避免为了单个功能暴露整个 Manager 或运行时对象内部状态。

## 旧逻辑兼容与迁移约束
- 保持当前可运行逻辑，不一次性删除或重写现有 `Player`、`Enemy`、`SkillShoot`、`EnemyGenerateManager` 等已在场景中使用的脚本。
- 新系统先以适配器、旁路入口或可切换开关接入；确认新流程可运行后，再逐步迁移旧职责。
- 每次迁移只替换一个清晰职责，例如目标选择、伤害结算、对象生成、UI 刷新或配置读取，避免跨系统大改。
- 迁移期间必须保持旧 Prefab、Animator、Collider、Layer、Tag 和 Inspector 序列化字段不失效。
- 删除旧代码前必须确认没有场景引用、Prefab 引用和运行时调用；无法确认时保留兼容层并标注后续清理任务。

