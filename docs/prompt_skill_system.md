# Skill System 模块需求提示词

## 目标
实现数据驱动技能系统，支持自动释放、多段伤害、DOT、范围伤害、连锁、穿透、弹射、暴击、属性伤害、技能进化和等级成长。

## 当前基础
已有 `PlayerSkillManager`、`SkillBase`、`SkillShoot`、技能等级数据和子弹生成逻辑。当前技能主要围绕射击技能，技能冷却、目标扫描和弹道参数还未完全配置化。

## 输出要求
- 生成 `SkillDataSO`：ID、名称、图标、技能类型、元素、冷却、范围、目标策略、等级数据。
- 生成 `SkillRuntime`：运行时冷却、等级、释放次数、临时 Modifier。
- 生成 `SkillManager`：持有已解锁技能、自动释放、升级、进化。
- 生成技能效果接口：`ISkillEffect`，支持伤害、DOT、召唤、护盾、治疗。
- 支持技能等级表和进化条件。

## 实现流程
1. 先把 `SkillShoot` 配置化，作为标准技能模板。
2. 将目标选择抽离给 Targeting System。
3. 技能释放只生成 `DamageInfo` 和 `ProjectileSpawnRequest`，不直接扣血。
4. 技能升级由 Upgrade/Buff 系统修改 RuntimeData。
5. 技能释放、命中、升级、进化全部发布事件。

## 技能流
冷却就绪 -> 选择目标 -> 创建释放请求 -> 生成投射物/范围效果 -> DamageSystem 结算 -> Buff/DOT 附加 -> UI 刷新。

## 验收标准
- 新增技能只需新增配置和效果类，不修改 `SkillManager` 主流程。
- 技能可以被 Buff 改变伤害、范围、冷却、弹道数和持续时间。
- 自动释放不会每帧产生 GC。
