# MainScene UI 架构重构说明

> 总流程见 `ui_architecture_workflow.md` | 实现约束见 `prompt_main_scene_ui.md`

## 场景层级（目标结构）

```
MainSceneUI [Canvas, CanvasScaler, GraphicRaycaster, MainSceneView]
├── SpaceCityBackground [Image]
├── TopProfile [MainSceneProfilePanel]
│   ├── AvatarFrame / AvatarImage / PlayerLevelText / …
│   └── ExpSlider
├── TopResources [MainSceneResourcePanel]
│   ├── DiamondResource [UI_ItemSlot]
│   ├── GoldResource [UI_ItemSlot]
│   ├── EnergyResource [UI_ItemSlot]
│   └── TicketResource [UI_ItemSlot]
├── TopSystemIcons [MainSceneCardPanel]  ← mail, social, settings
├── PromotionIcons [MainSceneCardPanel]  ← sign-in banner, 首充/月卡/…
├── LeftSideIcons [MainSceneCardPanel]
├── RightSideIcons [MainSceneCardPanel]
├── RewardRow [MainSceneRewardPanel]
│   ├── OnlineReward [GeneralRewardCardPanel]
│   ├── StageReward [GeneralRewardCardPanel]
│   └── OfflineReward [GeneralRewardCardPanel]
├── StartBattle [GeneralCardPanel]       ← 也可由 CardPanel 引用
├── BottomNav [MainSceneCardPanel]
└── StatusText [TMP_Text]
```

`MainSceneView` 只持有上述 Panel 引用 + `statusText` + 战斗场景配置，不再直接持有几十个 `MainSceneIconButtonBinding`。

## 脚本职责

| 脚本 | 挂载位置 | 职责 |
|------|----------|------|
| `MainSceneView` | `MainSceneUI` 根 | 事件订阅、`HandleAction`、进战斗、状态栏 |
| `MainSceneProfilePanel` | `TopProfile` | 昵称/VIP/等级/经验条刷新 |
| `MainSceneResourcePanel` | `TopResources` | 四类资源数值、加号占位回调 |
| `MainSceneCardPanel` | 各图标区域父节点 | 注册 `GeneralCardPanel` 点击，汇总 `CardClicked` |
| `MainSceneRewardPanel` | `RewardRow` | 三奖励条文案与红点 |

## MainSceneAction 分发

```
GeneralCardPanel.OnClick
  → MainSceneCardPanel.RaiseCardClicked(action)
    → MainSceneView.HandleAction(action)
```

奖励条：

```
GeneralRewardCardPanel.OnClick
  → MainSceneRewardPanel.RaiseRewardClicked(action)
    → MainSceneView.HandleAction(action)
```

## 红点策略

| 入口 | 条件（当前样板） |
|------|------------------|
| 签到 Banner / 每日签到 | `DailyRewardManager.CanClaimToday()` |
| 成就 | `AchievementManager.HasClaimableRewards()` |
| 在线奖励 | 同签到可领 |
| 离线收益 | `ShopManager.CanClaimFreeDiamond` |
| 邮件/社交/促销 | 样板固定 true，待接通知系统 |

由 `MainSceneView.RefreshRedDots()` 调用各 Panel 的 `ApplyRedDots(...)`。

## MainSceneBuilder 迁移

Editor 菜单 `Attack Barbarians/UI/Create MainScene` 在生成控件时：

1. 为图标按钮根物体添加 `GeneralCardPanel` 并写入 action/控件引用。
2. 为资源条添加 `UI_ItemSlot`。
3. 为奖励条添加 `GeneralRewardCardPanel`。
4. 创建区域父节点并挂子 Panel，最后绑定 `MainSceneView` 的 Panel 字段。

**不要求**在 Play 模式用代码创建 UI；Builder 仅用于首次生成场景。

## 测试步骤

1. 菜单执行 `Create MainScene`（或打开已有 `Assets/Scenes/MainScene.unity`）。
2. Play：`GameBootstrapper` + `MainSceneView` 显示默认资源与红点。
3. 点击「签到/在线奖励」验证 `DailyRewardManager` 与状态栏。
4. 点击「离线收益」验证 `ShopManager.TryClaimFreeDiamond`。
5. 点击「开始战斗」进入 `BattleScene`。
6. 在 Inspector 中替换任一 `GeneralCardPanel` 的 Sprite，确认无需改代码。

## 后续

- 将 `MainSceneCardPanel` 的促销区拆为独立 Prefab（月卡等大 Banner）。
- 接入全局 `RedDotService` 替代 Presenter 内硬编码。
- 商城/角色等底栏切页改为 `UIManager` + `UiPanelBase` 子面板。
