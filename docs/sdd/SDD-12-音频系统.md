# SDD-12 音频系统

| 项 | 内容 |
|----|------|
| 版本 | v1.0 |
| 关联 PRD | §13 音频系统 |
| 架构层 | L4 Presentation |
| 实现度 | ⚠️ 60% |

---

## 1. 系统概述

BGM/SFX 双通道、对象池播放、订阅 `GameEvents` 按状态切 BGM，读取 `SettingsData` 音量。

---

## 2. 核心类

| 类 | 路径 |
|----|------|
| `AudioManager` | `Audio/AudioManager.cs` |
| `AudioDatabaseSO` | `Audio/AudioDatabaseSO.cs` |
| `AudioConfigSO` | `Audio/AudioConfigSO.cs` |
| `AudioSourcePool` | `Audio/AudioSourcePool.cs` |

---

## 3. 音频 ID（GameConstants.AudioIds）

### BGM

main_menu · gameplay · paused · game_over · upgrade · boss

### SFX

ui_click · ui_confirm · player_hurt · enemy_hit · enemy_kill · skill_cast · boss_*

---

## 4. 触发

| 事件 | 音频 |
|------|------|
| GameState → Playing | gameplay BGM |
| GameState → Paused | paused BGM |
| Boss.Spawned | boss BGM |
| 技能施法 | skill_cast SFX |

---

## 5. 实现状态分析

| 项 | 状态 |
|----|------|
| Manager + 池化 | ✅ |
| 读 SettingsData 音量 | ✅ |
| AudioDatabase 资产 | ⚠️ 可能缺失，运行静音警告 |
| 设置 UI 调音量 | ❌ |
| 全按钮 SFX 绑定 | ⚠️ 部分 |

---

## 6. 测试要点

- 状态切换 BGM 交叉淡化  
- 音量 0 时静音  
- 执行 `AudioConfigBootstrapMenu` 后有声  
