# Auto Attack System

## 职责
- `AutoAttackDataSO`：连发上限、休整秒数、扫描间隔、每发弹道数、扇形角度、投射物 SO、技能 ID。
- `AutoAttackController`：目标刷新、连发计数、调用 `ProjectileManager`、发布 `PlayerAttackStarted` / `PlayerSkillCast`。

## 数据流
`PlayerIdleState (CanAttack)` → `PlayerShootState (动画帧)` → `AutoAttackController.ExecuteAttack` → `ProjectileManager` → `DamageSystem`

## 场景挂载
1. **Player** 根物体添加 `AutoAttackController`（`BattleScene` 已挂载示例）。
2. **Fire Origin** 拖入射击发射点 Transform（通常为 `SkillManager` 子节点，与 `SkillShoot.bulletSpawnPoint` 一致）。
3. 可选：将 `Assets/Resources/Config/AutoAttack/AutoAttack_Default.asset` 加入 `ConfigDatabase.autoAttacks` 列表。

## 测试步骤
1. 进入 `BattleScene`，确认 `GameBootstrapper`、`ProjectileManager` 已引导。
2. 生成敌人进入玩家射程，观察 Idle → Shoot 切换与子弹从池飞出。
3. 连续射击至连发上限（默认 10），应停止攻击约 4 秒（`burstRecoverySeconds`）后恢复。
4. 敌人清空后应回到 Idle，Console 无 `AutoAttackDataSO` / `ProjectileManager` 相关错误。

## 已迁移的旧逻辑
| 旧代码 | 新归属 |
|--------|--------|
| `SkillShoot` 敌人扫描（启用 AutoAttack 时） | `AutoAttackController` + `PlayerTargetScanner` |
| `SkillShoot.ActivateOneShootAttack` 子弹生成 | `AutoAttackController.FireAtTarget` → `ProjectileManager` |
| `SkillShoot` 连发/休整计数 | `AutoAttackDataSO` + `AutoAttackController` burst 状态 |

## 未迁移边界
- `SkillShoot` 仍保留 `bulletWaveList` / `SkillObject_BulletSpawn` 路径（无 `AutoAttackController` 时）。
- 多波次排布、`shootLine` 角度表仍待 Weapon / Skill 配置化。
- 主动技能、非射击类自动释放归属 Skill System（第三阶段）。

## 扩展点
- 新武器：复制 `AutoAttackDataSO`，调整 `projectilesPerShot` / `SpawnPattern`。
- 多武器槽：Player 挂多个 `AutoAttackController` 或后续 `WeaponController` 持有本组件。
- Buff 改攻速：经 `PlayerRuntimeStats` → `AnimSpeedMultiplier` 已联动 Animator。
