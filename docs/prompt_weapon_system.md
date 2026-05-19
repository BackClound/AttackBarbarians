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
