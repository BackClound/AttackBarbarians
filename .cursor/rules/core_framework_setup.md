---
description: "Core Framework Unity 场景挂载与类使用说明"
globs: ["Assets/Scripts/Core/**/*.cs", "Assets/Scripts/Managers/**/*.cs", "Assets/Scripts/Pool/PoolManager.cs", "Assets/Scripts/Data/GameConfig.cs"]
alwaysApply: false
---

# Core Framework 挂载规则

生成或修改以下脚本时，必须为每个 public 类型添加 `/// <summary>` 与 `<remarks>`，包含：**是否需要挂载**、**推荐 GameObject 名称**、**禁止挂载位置**、**获取方式**。

## 必须挂场景的 MonoBehaviour

| 类 | 物体 |
|----|------|
| GameBootstrapper | GameSystems（唯一） |
| ConfigManager | GameSystems |
| GameManager | GameSystems 或子物体 GameManager |
| PoolManager | GameSystems/PoolRoot |

## 禁止挂场景

EventBus、ServiceLocator、GameConstants、GameState、GameStateChange、GameEventContext、IGameSystem

## ScriptableObject

GameConfig → Assets/Resources/Config/GameConfig.asset

## 交付清单

- 更新 docs/core_framework_scene_setup.md（如有新 Manager）
- 在 PR/说明中列出 Hierarchy 与 Inspector 配置
- 不破坏 Player、EnemyGenerateManager 等已有场景引用

完整文档：`docs/core_framework_scene_setup.md`
