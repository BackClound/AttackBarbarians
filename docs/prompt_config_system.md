# Config System 模块需求提示词

## 目标
建立 ScriptableObject 配置系统，让角色、敌人、技能、Buff、波次、Boss、掉落、音频、UI 文案和数值曲线全部可配置。

## 当前基础
`Stats` 和 `SkillSystem/Data` 已有少量数据类，但多数平衡参数仍写在 MonoBehaviour 或 Inspector 字段中，尚未形成统一配置入口。

## 输出要求
- 生成 `ConfigManager`：集中加载和缓存配置。
- 生成基础配置类型：`PlayerDataSO`、`EnemyDataSO`、`SkillDataSO`、`BuffDataSO`、`WaveDataSO`、`BossDataSO`、`DropTableSO`。
- 生成 `StatModifierConfig`：支持加法、乘法、最终倍率。
- 生成配置校验工具：检查空引用、负数、重复 ID、缺失图标和非法等级。
- 输出配置资产命名规范和存放路径。

## 实现流程
1. 先定义稳定 ID：`configId` 用于存档、事件和调试。
2. 将数值和展示字段放入 SO，将运行时状态留在 Controller。
3. 避免 SO 在运行时被直接修改，使用 RuntimeData 复制状态。
4. 为每类配置提供编辑器校验或运行时断言。

## 数据流
`ConfigManager` 加载 SO -> Manager 读取配置 -> Controller 创建 RuntimeData -> Event/UI 使用 RuntimeData 展示。

## 验收标准
- 新增敌人、技能、Buff 不需要改核心逻辑。
- 存档只保存 ID 和等级，不保存 SO 引用。
- 配置错误能在启动或编辑器阶段明确暴露。

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

