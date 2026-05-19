# Buff System 模块需求提示词

## 目标
实现可配置 Buff 系统，支持叠加、永久 Buff、临时 Buff、被动 Buff、光环 Buff、技能 Buff、敌人减益和局外成长。

## 当前基础
`SkillBuff核心机制.md` 已描述升级选单和 BuffSO，但代码中尚未形成 Modifier 管线，`Stat.GetFinalValue()` 仍偏基础值返回。

## 输出要求
- 生成 `BuffDataSO`：ID、名称、图标、描述、类型、目标、持续时间、最大层数、数值 Modifier。
- 生成 `BuffRuntime`：当前层数、剩余时间、来源、目标。
- 生成 `BuffManager`：添加、刷新、叠加、移除、Tick、清理。
- 生成 `IBuffEffect`：应用属性、技能参数、DOT、控制、光环等效果。
- 支持 Player Buff、Enemy Debuff、Skill Buff、Permanent Buff。

## 实现流程
1. 定义 Buff 类型和叠加规则。
2. 将属性加成接入 `Entity_Stats` 或新的 RuntimeStats。
3. DOT 和控制类 Buff 通过 Tick 或事件触发。
4. Buff 添加/移除时发布事件，UI 展示图标和层数。
5. 存档只保存需要持久化的 Buff ID、等级和层数。

## 数据流
`UpgradeManager/Skill/DamageSystem -> BuffManager -> RuntimeStats/SkillRuntime -> GameEvents.OnBuffChanged`

## 验收标准
- 同类 Buff 可按配置叠加或刷新。
- 临时 Buff 到期自动移除并还原属性。
- 永久 Buff 可保存并在游戏启动时恢复。
