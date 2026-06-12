# SDD-11 UI 系统

| 项 | 内容 |
|----|------|
| 版本 | v1.0 |
| 关联 PRD | §12 UI 系统 |
| 架构层 | L4 Presentation |
| 实现度 | ⚠️ 75% |

---

## 1. 系统概述

Presenter 模式 UI：UIManager 状态驱动、MainScene 双页、BattleScene HUD 与弹层，Inspector 绑定控件。

**规范**：`docs/version_02/ui_architecture_workflow.md`

---

## 2. 核心类

| 类 | 路径 |
|----|------|
| `UIManager` | `UI/UIManager.cs` |
| `UiPanelBase` | `UI/Core/UiPanelBase.cs` |
| `UiSafeAreaFitter` | `UI/Core/UiSafeAreaFitter.cs` |
| `UiTechWastelandPalette` | `UI/Core/UiTechWastelandPalette.cs` |
| `UiTmpChineseFont` | `UI/Core/UiTmpChineseFont.cs` |

---

## 3. 面板清单

### BattleScene

| 面板 | 脚本 | 状态 |
|------|------|------|
| 主菜单 | MainMenuPanelUI | ✅ |
| HUD | GameplayHudPresenter | ✅ |
| 暂停 | PausePanelUI | ✅ |
| 三选一 | UpgradePanelUI | ✅ |
| 波次过渡 | WaveTransitionPanelUI | ✅ |
| 结算 | GameOverPanelUI | ⚠️ 缺卡列表 |
| 局内商店 | ShopPanelUI | ✅ |
| 签到 | DailyRewardPanelUI | ⚠️ 缺 7 日格 |
| 成就 | AchievementPanelUI | ✅ |
| Boss HUD | BossHudPresenter | ⚠️ |

### MainScene

| 面板 | 脚本 | 状态 |
|------|------|------|
| 页面导航 | MainSceneView | ✅ |
| 战斗页 | MainSceneBattlePageView | ✅ |
| 商城页 | ShopSceneView | ✅ |
| 开箱弹窗 | ShopCrateRewardPopupPanel | ✅ |
| 奖池预览 | ShopCratePoolPreviewPanel | ✅ |
| Meta 奖励页 | MetaRewardPagePanel | ⚠️ 场景绑定 |
| 发卡弹窗 | UpgradeCardRewardPopupPanel | ⚠️ |
| 顶栏 | TopResourceBarPanel | ✅ |

### Widget

`UpgradeCardDisplayView` · `ShopCrateWidget` · `UI_RedDot` · `GeneralCardPanel`

---

## 4. 页面流

```
MainScene: Home ↔ Shop (BottomNav)
  Home → 开战 → BattleScene
  Shop → 宝箱/补给/兑换

BattleScene: HUD ↔ Pause / Upgrade / GameOver
```

---

## 5. Editor 构建

| 菜单 | 脚本 |
|------|------|
| Create MainScene | MainSceneBuilder.cs |
| Build Shop Page | ShopPageBuilder.cs |
| Build Shop Crate Popups | ShopCratePopupBuilder.cs |

---

## 6. 实现状态分析

| 项 | 状态 |
|----|------|
| UIManager + 状态驱动 | ✅ |
| SafeArea / 中文 TMP | ✅ |
| MainScene 双页 | ✅ |
| 宝箱开奖 UX | ✅ |
| 科技废土正式美术 | ⚠️ 占位 |
| 设置/成长页 | ❌ |
| 全局 RedDotService | ❌ |
| Meta 面板 Inspector 绑定 | ⚠️ |

---

## 7. 测试要点

- 各 GameState 下面板显隐正确  
- 体力不足开战提示  
- 宝箱十连滚动展示  
