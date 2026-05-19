# Save System 模块说明

## 概述

JSON 本地持久化，保存局外成长、货币、技能等级、设置、签到与统计，以及可选的局内进度。通过 `GameEvents` 在加载/保存完成时广播，由波次结算、升级确认、GameOver、退出等时机触发写入。

## 核心类型

| 类型 | 挂载 | 职责 |
|------|------|------|
| `SaveData` | 否 | 根存档结构 |
| `SettingsData` | 否 | 音量、画质、语言、触控 |
| `RunProgressData` | 否 | 局内波次/等级/血量/Buff |
| `SaveStatisticsData` | 否 | 累计统计 |
| `ConfigIdIntPair` | 否 | configId + 整型值列表元素 |
| `SaveVersionMigrator` | 否 | 版本迁移 |
| `SaveFileIO` | 否 | 读写主档/备份 |
| `SaveManager` | `GameSystems` | Load/Save/AutoSave/Delete/Export/Import |

## 存储路径

- 主档：`Application.persistentDataPath/save.json`（文件名可在 `GameConfig.SaveFileName` 覆盖）
- 备份：`save.json.bak`（每次成功写入前复制主档）
- 导出：`save_export.json`（`ExportToFile`）

## 启动顺序

`ConfigManager` → **`SaveManager.Load`** → `EventBus` → `PoolManager` → `GameManager` → `GameFlowManager`

## 事件

| Key | 发布方 | 触发时机 |
|-----|--------|----------|
| `Save.Loaded` | `SaveManager` | `Initialize` / `Load` 完成后 |
| `Save.Completed` | `SaveManager` | 磁盘写入成功后 |

订阅示例：

```csharp
GameEvents.SubscribeSaveLoaded(OnSaveLoaded);
GameEvents.SubscribeSaveCompleted(OnSaveCompleted);
// OnDestroy: UnsubscribeSaveLoaded / UnsubscribeSaveCompleted
```

## 自动保存触发

| 时机 | 行为 |
|------|------|
| `Wave.Completed` | 更新 `runProgress.currentWave`，防抖 AutoSave |
| `Upgrade.SelectionCompleted` | 防抖 AutoSave |
| `Game.Over` | 清除局内进度，`SaveImmediate` |
| `GameState.Exiting` | `SaveImmediate` |
| `OnApplicationPause(true)` / `OnApplicationQuit` | 若有 dirty 则立即写盘 |
| `Shutdown` | 若有 dirty 则写盘 |

## API 摘要

```csharp
var save = ServiceLocator.Get<SaveManager>();
save.Current.SetSkillLevel(GameConstants.ConfigIds.SkillShoot, 3);
save.Gold += 100;
save.Settings.musicVolume = 0.5f;
save.MarkDirty();
save.SaveImmediate();

save.ExportToJson();
save.ImportFromJson(json);
save.DeleteSave();
```

## 版本迁移

- 当前版本：`SaveConstants.CurrentVersion`（= 1）
- 加载后由 `SaveVersionMigrator.Migrate` 补齐缺失子对象；未知版本会重置为默认档

## 场景挂载

1. 在 `GameSystems` 上 Add Component → `SaveManager`（或由 `GameBootstrapper` 自动 AddComponent）。
2. 可选：将 `SaveManager` 拖入 `GameBootstrapper` 的 **Save Manager** 槽位。
3. `GameConfig.asset` 中可配置 `Enable Auto Save`、`Auto Save Debounce Seconds`、`Save File Name`。

## 手动验证步骤

1. 运行场景，Console 出现 `[SaveManager] 已加载`（开启 `Enable Runtime Logs` 时）。
2. Inspector 右键 `SaveManager` → **Debug/Save**，检查 `persistentDataPath` 下生成 `save.json`。
3. 修改 `Current.settings.musicVolume` 后 Save，重启 Play，确认设置仍在。
4. **Debug/Delete** 后应重建默认档且 `gold == 0`。
5. 令 Player 死亡进入 `GameOver`：应写盘并发布 `Save.Completed`（可用临时订阅脚本验证）。
6. `GameFlowManager` → **Debug/Simulate Wave Completed**：应触发防抖保存（日志 `AutoSave`）。

## 与旧逻辑边界

| 仍由旧逻辑负责 | 本模块已提供、待迁移 |
|----------------|----------------------|
| `Player`、`SkillShoot` Inspector 数值 | `SaveData.skillLevels` + configId |
| 无持久化 | `gold`/`diamonds`/永久升级/天赋/装备 |
| `GameFlowManager` 不再直接 `RaiseSaveCompleted` | `SaveManager` 写盘成功后发布 |
| Player 死亡流程不变 | 通过 `Game.Over` 事件触发存档 |

**未改动**：`Player`、`EnemyGenerateManager`、`SkillShoot` 行为与 Prefab；存档为旁路，玩法系统按需读取 `ServiceLocator.Get<SaveManager>()`。

## 后续任务

- `ResourceManager` / Shop 写入 `gold`、`diamonds`
- Talent/Equipment Manager 读写 `talentLevels`、`equipmentLevels`
- AudioManager 应用 `SettingsData` 音量
- WaveManager 完成后填充 `RunProgressData` 与 Buff 列表
- Achievement/DailyReward 写入签到字段
