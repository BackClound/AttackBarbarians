# Content 与 Map System 模块需求提示词

## 目标
实现内容扩展与地图系统，支持不同地图、敌人生态、事件、特殊波次和后续 Addressables 内容加载。

## 当前基础
`work_flow.md` 第四阶段包含 More Skills、Maps、Events，`总结文档.md` 提到怪物生态、波次设计、Boss AI 和 Addressables 热更新架构。

## 输出要求
- 生成 `MapDataSO`：地图 ID、背景、出生区域、墙体位置、推荐难度、BGM。
- 生成 `MapManager`：加载地图、设置相机边界、初始化生成区域。
- 生成 `GameplayEventDataSO`：随机事件 ID、触发条件、持续时间、效果。
- 生成 `ContentRegistry`：登记敌人、技能、Boss、地图和事件配置。
- 预留 Addressables 加载接口，但不强制引入复杂热更新。

## 实现流程
1. 先将当前固定战斗场景抽象为默认地图配置。
2. WaveManager 从 MapDataSO 读取生成区域和地图修正。
3. 随机事件通过 EventBus 影响 Wave、Buff、Enemy 或 Reward。
4. 内容配置集中校验，避免缺失 Prefab 或重复 ID。

## 数据流
`MapDataSO -> MapManager -> SpawnArea/WaveManager/UI/Audio`

## 验收标准
- 默认地图可配置生成范围和背景。
- 新增地图不需要修改核心战斗逻辑。
- 随机事件能在指定条件下触发并结束。

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

