---
description: "Unity 2D 无限防守模式+肉鸽游戏项目全局规范"
globs: ""
alwaysApply: true
---

# 项目概述
这是一个 Unity 2D 无限防守模式+ 肉鸽（Roguelike / Roguelite）元素游戏。

## 游戏核心玩法
- 游戏类型为无限防守模式，每波会随机产生某一范围内的enemy，当Player击败所有enemy或者到达30S之后，自动进入下一波。
- 该游戏为竖屏游戏，Player位于屏幕下侧进行防守，enemy从屏幕上方随机生成，enemy直线向下接近Player，然后进行攻击。
- 肉鸽元素：每波结束后获得随机 Buff选择，提升Player的属性或技能等级，具有高度重复可玩性。
- 核心机制包括：Player的攻击逻辑、敌人的波次生成、随机升级池、资源管理、肉鸽风格的永久成长系统。

## 技术栈
- 引擎：Unity 6000.3.6f1 LTS（推荐，稳定且长期支持）
- 渲染：URP（通用渲染管线）以获得最佳 2D 渲染性能和后处理效果
- 语言：C#（.NET Standard 2.1）
- 版本管理：使用程序集定义（Assembly Definition）组织代码
- 外部依赖：尽量不引入第三方插件，优先使用 Unity 原生功能

## 命名规范
- 脚本文件名与类名一致，使用 PascalCase。
- UI 元素命名格式：类型_名称（如 `Btn_Start`、`Txt_Score`、`Img_Health`）。
- 场景对象命名格式：功能描述（如 `SkillObject_Bat`、`Enemy_Spawner`）。
- Prefab 命名格式：类型_名称.prefab（如 `Skill_Bullet.prefab`、`Enemy_Bat_01.prefab`）。
- ScriptableObject 资产命名格式：数据类别_名称.asset（如 `SkillBuffSo.asset` 、 `BaseBuffSO.asset`）。

## 目录结构规则
- 脚本按系统功能模块分类存放于 `Assets/Scripts/`：
  - `Core/`：游戏核心系统（GameManager、WaveManager、UpgradeManager 等）
  - `Player/`：Player相关（Player、PlayerDataSO、Player_Health、PlayerShootState 等）
  - `Enemies/`：敌人相关（Enemy、EnemyDataSO、EnemySpawner 等）
  - `Bullets/`：子弹/投射物相关（Bullet类）
  - `Roguelike/`：肉鸽系统（UpgradeManager、BuffDataSO、SkillBuff、 BaseBuff、Buff 效果实现）
  - `UI/`：UI 相关（UIManager、各界面面板）
  - `Managers/`：全局管理器（AudioManager、PoolManager、ResourceManager）
  - `Data/`：ScriptableObject 数据定义
  - `Utils/`：工具类（ExtensionMethods、MathHelper 等）
  - `Pool/`：对象池系统
- 资源文件按类别存放于 `Assets/Art/`、`Assets/Prefabs/`、`Assets/Audio/` 等目录。

## 编译与性能
- 使用程序集定义（Assembly Definition）文件组织代码，减少不必要的编译范围。
- 大世界地图使用 Tilemap 系统构建，搭配 Grid 和 TilemapRenderer。
- 遵循数据驱动设计原则：所有塔和敌人的数值通过 ScriptableObject 配置，便于后期调优和扩展。
- 优先使用对象池管理频繁生成的对象（子弹、特效、敌人实例）。