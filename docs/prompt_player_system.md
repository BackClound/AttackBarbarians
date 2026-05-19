# Player System 模块需求提示词

## 目标
完善 Player 防守核心，支持基础属性、自动攻击、技能释放、受击死亡、Buff 应用、永久成长和移动端适配。

## 当前基础
`Player` 已有单例、状态机、Idle/Shoot/Dead 状态、`Player_Health`、`PlayerCombat` 和 `SkillShoot` 调用链。当前仍依赖直接引用和单例，目标扫描逻辑与技能逻辑存在重复。

## 输出要求
- 生成或改造 `PlayerController`：负责运行时行为和状态机。
- 生成 `PlayerDataSO`：配置生命、攻击、攻速、射程、暴击、初始技能。
- 生成 `PlayerRuntimeStats`：整合基础属性、局内 Buff、永久成长。
- 统一目标选择策略：最近、血量最低、距离墙体最近、Boss 优先。
- Player 事件：血量变化、死亡、攻击开始、技能释放、属性变化。

## 实现流程
1. 保留现有 Player 可运行状态，不大拆状态机。
2. 抽出目标扫描服务，供 PlayerCombat 和 Skill 使用。
3. 将属性读取统一指向 RuntimeStats。
4. 将血量 UI 和死亡流程改为事件驱动。
5. 为 Buff/Upgrade 留出 `ApplyModifier` 入口。

## 数据流
`PlayerDataSO + SaveData + BuffRuntimeData -> PlayerRuntimeStats -> PlayerController/SkillManager -> DamageSystem`

## 验收标准
- Player 可以自动选择目标并释放基础技能。
- Player 属性变化能即时影响攻击、血量、技能效果。
- UI 不直接读取 Player 单例也能刷新血量和状态。
