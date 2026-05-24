# Talent System

局外天赋成长：配置驱动、存档持久化、战斗前合并到 `PlayerRuntimeStats`。

## 架构

| 组件 | 挂载 | 职责 |
|------|------|------|
| `TalentDataSO` | 无（`Resources/Config/Talent/`） | 天赋 ID、等级上限、金币消耗、前置、每级属性修正 |
| `TalentManager` | `GameSystems` | 升级、读存档、聚合修正、`GameEvents.TalentChanged` |
| `PlayerRuntimeStats` | 无 | `SetTalentModifiers` 与 Buff/局内修正叠加 |

## 数据流

`SaveData.talentLevels → TalentManager → PlayerRuntimeStats → PlayerController.RefreshEntityStats`

## 事件

- `GameConstants.EventKeys.TalentChanged` / `GameEvents.RaiseTalentChanged`

## 场景

在 `GameSystems` 上挂 `TalentManager`（或由 `GameBootstrapper` 自动 AddComponent）。`ConfigDatabase` 需引用天赋资产。

## 测试

1. 运行战斗场景，Console 确认 ConfigManager 加载 `Talent=2`。
2. 选中 `TalentManager` → Debug/Upgrade Max Hp Talent（需足够 `SaveManager.Gold`）。
3. 重启游戏，确认 `save.json` 中 `talentLevels` 保留且 Player 最大生命上升。

## 射击技能（同期变更）

`SkillType.Shoot` 与闪电/冰霜一致：由 `SkillManager.Update` 在冷却就绪且 `AutoCast` 时调用 `ShootSkillEffect.TryAutoCast`，不再依赖 `PlayerShootState` 动画攻击帧。`CanEnterCombatState` 在射击技能已解锁时返回 `false`，避免进入射击动画状态。
