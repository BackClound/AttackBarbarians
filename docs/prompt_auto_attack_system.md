# Auto Attack System 模块需求提示词

## 目标
实现玩家自动攻击核心闭环：目标扫描调度、连发/休整、投射物生成与事件发布，作为 Weapon System 落地前的基础武器层。

## 当前基础
- `PlayerController` + `PlayerTargetScanner` 已统一目标策略。
- `ProjectileManager` + `ProjectileController` 已承接子弹生成与伤害。
- `PlayerIdleState` / `PlayerShootState` + `SkillShoot` 仍保留旧连发与 `SkillObject_BulletSpawn` 兼容路径。

## 输出要求
- `AutoAttackDataSO`：连发次数、休整时间、扫描间隔、弹道排布、绑定技能 ID、投射物配置。
- `AutoAttackController`：挂在 Player，负责 `CanAttack`、`ExecuteAttack`、目标列表兼容接口。
- 状态机与 `SkillShoot` 在存在 `AutoAttackController` 时优先走新路径。

## 数据流
`PlayerDataSO → PlayerRuntimeStats → PlayerTargetScanner → AutoAttackController → ProjectileSpawnRequest → ProjectileManager → DamageSystem`

## 验收标准
- 射程内有敌人时 Player 自动进入射击动画并发射投射物。
- 连发耗尽后进入配置化休整，休整结束可再次攻击。
- 无目标时不进入 Shoot 状态；扫描使用间隔调度，避免每帧全量扫描。
- `SkillShoot` 旧路径在未挂载 `AutoAttackController` 时仍可运行。

## 模块依赖边界
- 依赖：Config、Player、Projectile、Damage、GameEvents。
- 不依赖：UI、Buff 具体实现、Upgrade、Shop。
- 不直接 `Instantiate` 子弹；必须经 `ProjectileManager`。
