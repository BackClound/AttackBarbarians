# Weapon System 模块需求提示词

## 目标
实现 Weapon System，作为 Player 自动攻击和技能释放之间的桥接层，支持武器配置、攻击模式、成长、强化和后续装备系统。

## 当前基础
当前 Player 主要通过 `SkillShoot` 直接发射子弹，尚未区分武器、技能和投射物。项目规则要求单独支持 Weapon System。

## 输出要求
- 生成 `WeaponDataSO`：ID、名称、图标、攻击间隔、基础伤害、范围、弹道配置、绑定技能。
- 生成 `WeaponController`：管理冷却、目标选择、攻击请求和动画触发。
- 生成 `WeaponRuntimeData`：等级、临时属性、当前冷却、强化条目。
- 支持武器类型：弓/弩/法杖/投掷/召唤器。
- 武器攻击通过 Skill/Projectile/Damage 系统完成，不直接扣血。

## 实现流程
1. 将当前基础射击抽象成默认武器。
2. Player 持有一个或多个 WeaponController。
3. Weapon 负责发起攻击，Skill 负责效果，Projectile 负责表现，Damage 负责结算。
4. Upgrade/Buff 可修改武器攻速、射程、弹道数和触发概率。

## 数据流
`PlayerController -> WeaponController -> SkillManager/ProjectileManager -> DamageSystem`

## 验收标准
- 默认武器可以替代现有基础射击链路。
- 新增武器不需要修改 Player 主逻辑。
- 武器属性变化能实时影响攻击频率和技能触发。

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

