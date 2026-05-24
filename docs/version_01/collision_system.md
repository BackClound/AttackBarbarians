# Collision System

## 职责
- `CollisionDataSO`：玩家扫描层、墙体层、投射物命中层、默认半径/射线长度、缓冲上限。
- `CollisionManager`：NonAlloc Overlap / Raycast、敌人/墙体解析、投射物命中校验。
- `CollisionQuery`：Bootstrap 未完成时的静态回退入口。
- `CollisionProfile`：实体探测点（可选，覆盖射线原点与距离）。

## 数据流
```
PlayerTargetScanner / ProjectileController / EnemyController
    → CollisionQuery / CollisionManager
    → Physics2D (NonAlloc)
    → Enemy / WallControlManager
    → DamagePipeline（伤害仍由 Damage 模块结算）
```

## 场景挂载
1. **GameSystems** 添加 `CollisionManager`（`GameBootstrapper` 可自动 AddComponent）。
2. 确认 `Assets/Resources/Config/Collision/Collision_Default.asset` 存在（Layer：Enemy=256，Wall=512，与 BattleScene 一致）。
3. 可选：敌人子节点挂 `CollisionProfile`（Role=WallSensor），覆盖 `attackCheck` 探测点。

## 状态机架构（Player / Enemy）
| 旧模式 | 新模式 |
|--------|--------|
| `Player.Update` 驱动状态机 | `PlayerController.TickStateMachine` |
| `BatEnemy.Update` 驱动状态机 | `EnemyController.TickStateMachine` |
| `PlayerCombat` 扫描敌人 | `PlayerController` + `PlayerTargetScanner` + Collision |
| `EnemyCombatManager` 射线+伤害 | `EnemyController.ExecuteWallAttack` |
| `Player_Health` 直接改 Slider | 仅 `GameEvents.PlayerHealthChanged` |

## 测试步骤
1. 进入 `BattleScene`，确认 Bootstrap 无 `CollisionManager` 报错。
2. 敌人在射程内：Player Idle→Shoot，子弹命中，飘字由 `DamageSystem` 事件驱动。
3. 敌人贴墙：攻击动画帧后城墙受击、Player 血量下降。
4. Console 无 `OverlapCircleAll` 相关 GC 尖刺（扫描走 NonAlloc）。

## 未迁移边界
- `SkillShoot` 的 `bulletWaveList` 路径在无 `AutoAttackController` 时仍可用。
- 近战/范围 AOE 命中盒尚未统一为 `CollisionProfile` Hitbox 流程。
