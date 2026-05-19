# Input System 模块需求提示词

## 目标
配置移动端优先的输入系统，支持暂停、返回、升级选择、设置、商店、音效开关和后续技能手动触发。

## 当前基础
`局内点击事件和输入系统.md` 已描述 Input Action Asset 和 `PlayerInputHandler`，但代码层尚未看到统一输入管理。

## 输出要求
- 生成 `GameInput.inputactions` 设计说明。
- 生成 `InputManager` 或 `PlayerInputHandler`：订阅输入并发布 GameEvents。
- Action Maps 包含 `Gameplay`、`UI`、`Debug`。
- 支持触屏点击、返回键、暂停键和 UI 导航。
- 输入启停由 `GameState` 控制。

## 实现流程
1. 定义输入事件，不让 UI 和战斗对象直接读取 Input。
2. `Playing` 状态启用 Gameplay，`Paused/UpgradeChoosing` 状态启用 UI。
3. 移动端按钮通过 Unity UI 事件调用同一套输入接口。
4. Debug 输入只在开发构建启用。

## 验收标准
- 暂停、返回、升级选择和设置按钮都能通过事件触发。
- 游戏暂停或升级选择时不会误触发战斗输入。
- 输入逻辑与 UI 展示解耦。
