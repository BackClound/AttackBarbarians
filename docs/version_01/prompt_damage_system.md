# Damage System 模块需求提示词

## 目标
建立统一伤害系统，支持普通伤害、暴击、护甲、元素伤害、DOT、范围伤害、穿透、弹射、减益和伤害飘字。

## 当前基础
当前 `IDamagable.TakeDamage(float damage)` 只传递数值，`AttackInfo` 尚未接入，暴击、元素、护甲、减益和攻击来源无法表达。

## 输出要求
- 生成 `DamageInfo`：攻击来源、目标、基础伤害、倍率、技能 ID、元素类型、是否暴击、DOT、击退、标签。
- 生成 `DamageResult`：最终伤害、是否击杀、是否暴击、被抵抗数值、触发效果。
- 生成 `DamageSystem`：统一计算伤害并发布事件。
- 扩展 `IDamageable`：接收 `DamageInfo`，返回 `DamageResult`。
- 生成 `ElementType`、`DamageType`、`DamageTag` 枚举。

## 实现流程
1. 保留旧 `TakeDamage(float)` 兼容入口，但内部转换成 `DamageInfo`。
2. 将 Player、Enemy、SkillObject 的伤害调用逐步迁移。
3. 在 DamageSystem 中集中处理暴击、护甲、元素、Buff Modifier。
4. 通过 `OnDamageApplied` 触发飘字、音效、受击反馈。

## 计算顺序
基础伤害 -> 技能倍率 -> 攻击方增益 -> 防御方减免 -> 元素修正 -> 暴击 -> 最终伤害 -> DOT/附加效果。

## 验收标准
- 所有伤害来源都能追踪到来源对象和技能 ID。
- 暴击、元素、护甲和 Buff 对最终伤害有统一影响。
- UI 飘字不再由 Enemy 直接调用。

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

