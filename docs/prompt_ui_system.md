# UI System 模块需求提示词

## 目标
实现完整 UI System，包括主菜单、HUD、血条、波次 UI、Buff UI、技能 UI、伤害飘字、升级三选一、暂停、商店和结算界面。

## 当前基础
已有 `DamageNumberController` 和 `DamageNumber`，`UI和游戏流程.md` 已描述 UIManager、HUD、UpgradePanel、PausePanel，但代码尚缺完整 UI 管理和事件绑定。

## 输出要求
- 生成 `UIManager`：管理所有面板切换。
- 生成 `GameplayHUD`：生命、经验、波次、金币、技能冷却、Buff 图标。
- 生成 `UpgradePanelUI`：展示三选一升级选项。
- 生成 `PausePanelUI`、`GameOverPanelUI`、`ShopPanelUI`。
- 生成 `DamageNumberView`：订阅 DamageEvent，使用对象池显示飘字。

## 实现流程
1. UI 只订阅事件和读取 ViewModel，不直接操控战斗对象。
2. `GameState` 驱动面板显示和 `Time.timeScale`。
3. 所有按钮通过 Input/UI 事件调用 Manager。
4. Canvas Scaler 使用 `Scale With Screen Size`，适配竖屏移动端。
5. 高频文本更新合并，避免每帧刷新所有 TMP 文本。

## 事件流
`OnPlayerHealthChanged` -> HUD 刷新生命。
`OnWaveStarted/Completed` -> HUD 刷新波次。
`OnUpgradeOptionsGenerated` -> UpgradePanel 显示卡片。
`OnDamageApplied` -> DamageNumberView 显示飘字。

## 验收标准
- UI 切换由游戏状态统一控制。
- 升级三选一选择后能恢复战斗。
- 移动端竖屏布局无关键按钮遮挡。
