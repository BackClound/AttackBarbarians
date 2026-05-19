# Talent 与 Equipment System 模块需求提示词

## 目标
实现局外成长中的天赋和装备系统，为 Player、Weapon、Skill、Buff 提供长期成长和 Build 差异。

## 当前基础
`work_flow.md` 将 Talent 和 Equipment 放在成长系统阶段，`project_rules.md` 要求支持永久成长，但当前代码尚未包含相关系统。

## 输出要求
- 生成 `TalentDataSO`：天赋 ID、名称、图标、等级上限、消耗、前置条件、属性效果。
- 生成 `TalentManager`：解锁、升级、应用全局属性。
- 生成 `EquipmentDataSO`：装备 ID、部位、品质、主属性、词条、套装。
- 生成 `EquipmentManager`：穿戴、卸下、强化、计算加成。
- 天赋和装备数据接入 Save System。

## 实现流程
1. 天赋和装备只影响 RuntimeStats，不直接修改配置资产。
2. 启动时从存档恢复成长数据。
3. 进入战斗前将局外加成合并到 PlayerRuntimeStats。
4. UI 通过事件刷新成长面板。

## 数据流
`SaveData -> TalentManager/EquipmentManager -> RuntimeStats -> Player/Skill/Weapon`

## 验收标准
- 天赋升级重启后仍保留。
- 装备穿戴能即时影响 Player 属性。
- 局外成长和局内 Buff 可叠加但来源可追踪。
