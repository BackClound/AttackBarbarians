# UI System 模块需求提示词

## 目标
实现完整 UI System，包括主菜单、HUD、血条、波次 UI、Buff UI、技能 UI、伤害飘字、升级三选一、暂停、商店和结算界面。

## 当前基础
已有 `DamageNumberController` 和 `DamageNumber`，`UI和游戏流程.md` 已描述 UIManager、HUD、UpgradePanel、PausePanel，但代码尚缺完整 UI 管理和事件绑定。

## 输出要求
- 生成 `UIManager`：管理所有面板切换。
- 生成 `GameplayHUD`：生命、经验、波次、金币、技能冷却、Buff 图标。
- 生成 `UpgradePanelUI`：展示三选一升级选项。
- 生成 `PausePanelUI`、`GameOverPanelUI`、`ShopPanelUI`。
- 生成 `DamageNumberView`：订阅 DamageEvent，使用对象池显示飘字。

## 实现流程
1. UI 只订阅事件和读取 ViewModel，不直接操控战斗对象。
2. `GameState` 驱动面板显示和 `Time.timeScale`。
3. 所有按钮通过 Input/UI 事件调用 Manager。
4. Canvas Scaler 使用 `Scale With Screen Size`，适配竖屏移动端。
5. 高频文本更新合并，避免每帧刷新所有 TMP 文本。

## 事件流
`OnPlayerHealthChanged` -> HUD 刷新生命。
`OnWaveStarted/Completed` -> HUD 刷新波次。
`OnUpgradeOptionsGenerated` -> UpgradePanel 显示卡片。
`OnDamageApplied` -> DamageNumberView 显示飘字。

## 验收标准
- UI 切换由游戏状态统一控制。
- 升级三选一选择后能恢复战斗。
- 移动端竖屏布局无关键按钮遮挡。

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

