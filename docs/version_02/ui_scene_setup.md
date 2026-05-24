# UI 场景挂载说明

> 对应脚本：`Assets/Scripts/UI/` | 视觉 Token：`UiTechWastelandPalette`

## 1. 创建 UI 根

1. 场景中新建 **Canvas**（Screen Space - Overlay），命名 `UICanvas`
2. 添加组件：
   - `CanvasScaler` + `UiCanvasScalerSetup`（1080×1920）
   - `GraphicRaycaster`
   - `UIManager`
   - `DamageNumberController`（飘字，可选子物体 `DamageNumberRoot`）

## 2. 面板层级（建议）

```
UICanvas
├── GameplayHUD          [GameplayHudPresenter]
├── MainMenuPanel        [MainMenuPanelUI, UiPanelTransition]  panelId=ui.main_menu
├── PausePanel           [PausePanelUI]                         panelId=ui.pause
├── GameOverPanel        [GameOverPanelUI]                      panelId=ui.game_over
├── UpgradePanel         [UpgradePanelUI]                       panelId=ui.upgrade
│   ├── Title
│   ├── ChoiceCard_0..2  [UpgradeChoiceCardView]
│   └── Scrim (Image alpha 0.62)
└── WaveTransitionPanel  [WaveTransitionPanelUI]                panelId=ui.wave_transition
```

各 `UiPanelBase` 的 **Panel Id** 字段填 `GameConstants.UiPanelIds` 对应值；**Root** 可指向自身。

## 3. GameplayHUD 绑定

| 字段 | 说明 |
|------|------|
| healthSlider / healthFill | HP 条 |
| expSlider / expFill | 经验条 |
| waveText | `WAVE-N` |
| timerText | 局内计时（RunSessionTracker） |
| goldText | SaveData.gold |
| killCountText | 本局击杀 |
| pauseButton | 调用 GameManager.PauseGame |

## 4. Upgrade 三选一

- `UpgradePanelUI.choiceCards` 拖 3 个 `UpgradeChoiceCardView`
- 卡片上挂 `UiRarityVisual` + 边框 Image
- 选择流程：`RandomRewardManager.TrySelectChoice` → `GameFlowManager.ConfirmUpgradeSelection`

## 5. 与 GameSystems 关系

- **不要**把 UIManager 挂在 GameSystems 上
- GameFlowManager 已通过 `RaiseUiPanelOpened/Closed` 驱动面板；UIManager 订阅这些事件 + `GameStateChanged`

## 6. 测试步骤

1. Play → HUD 显示波次/HP/EXP
2. 右键 GameFlowManager → `Debug/Simulate Wave Completed` → 波次过渡 → 三选一
3. 选卡后回到 Playing，Time.timeScale = 1
4. 暂停按钮 → 战术暂停 → 继续
5. 玩家死亡 → 结算面板显示时长/波次/击杀/奖励

## 7. 未完成 / Legacy

- Meta 商城、抽奖、签到、成长 Tab（P1）未实现
- 主菜单精英模式、设置页为占位日志
- Buff 图标行待接 BuffManager 快照
- 美术切图需按 `ui_icon_export_manifest.md` 替换占位 Sprite
