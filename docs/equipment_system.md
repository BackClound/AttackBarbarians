# Equipment System

局外装备成长：配置驱动、存档持久化、战斗前合并到 `PlayerRuntimeStats`，与天赋/Buff 来源分离可追踪。

## 架构

| 组件 | 挂载 | 职责 |
|------|------|------|
| `EquipmentDataSO` | 无（`Resources/Config/Equipment/`） | 部位、品质、主属性、词条、套装、强化参数 |
| `EquipmentManager` | `GameSystems` | 穿戴/卸下/强化、读存档、聚合修正、`GameEvents.EquipmentChanged` |
| `PlayerRuntimeStats` | 无 | `SetEquipmentModifiers` 与天赋/Buff/局内修正叠加 |
| `SaveData` | 无 | `equippedItems`（槽位→configId）、`equipmentLevels`（强化等级） |

## 数据流

`SaveData.equippedItems + equipmentLevels → EquipmentManager → PlayerRuntimeStats → PlayerController.RefreshEntityStats`

属性叠加顺序（`CollectAllModifiers`）：局内 extra → 天赋 → 装备 → Buff。

## 事件

- `GameConstants.EventKeys.EquipmentChanged` / `GameEvents.RaiseEquipmentChanged`
- Payload：`EquipmentChangedEventArgs`（`Equipped` / `Unequipped` / `Enhanced`）

## API 摘要

```csharp
var equip = ServiceLocator.Get<EquipmentManager>();
equip.TryEquip(GameConstants.ConfigIds.EquipmentWeaponBattleAxe);
equip.TryUnequip(EquipmentSlot.Weapon);
equip.TryEnhance(GameConstants.ConfigIds.EquipmentWeaponBattleAxe); // 须已穿戴
string id = equip.GetEquippedAt(EquipmentSlot.Weapon);
```

## 场景

在 `GameSystems` 上挂 `EquipmentManager`（或由 `GameBootstrapper` 自动 AddComponent）。`ConfigDatabase` 需引用 `Assets/Resources/Config/Equipment/` 下资产。

## 测试

1. 运行战斗场景，Console 确认 ConfigManager 加载 `Equipment=4`。
2. 选中 `EquipmentManager` → **Debug/Equip Default Weapon**（无需金币）。
3. 给 `SaveManager` 足够 `Gold` 后 → **Debug/Enhance Equipped Weapon**，确认伤害相关属性上升。
4. 穿戴 `Warrior Plate` + `Warrior Boots`，确认套装 2 件激活（额外 Damage%）。
5. 重启 Play，确认 `save.json` 中 `equippedItems`、`equipmentLevels` 保留。

## 与旧逻辑边界

- 不修改 `Player`、`SkillShoot`、敌人生成等战斗脚本行为。
- 无背包/掉落 UI；`TryEquip` 直接按 configId 穿戴（商店/掉落接入后再校验拥有权）。
- 强化仅允许当前槽位已穿戴的装备。

## 后续任务

- 背包与装备掉落接入 `DropTableSO`
- 成长面板 UI 订阅 `EquipmentChanged`
- 词条随机生成与洗练
