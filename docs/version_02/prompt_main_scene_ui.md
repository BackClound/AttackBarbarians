# Main Scene UI 实现 Prompt

## 目标
实现科幻主城首页 `MainScene`：顶部头像与资源栏、活动/签到/任务侧边入口、中心主视觉、奖励入口、开始战斗按钮、底部导航。

## UI 控件约束
- UI 脚本必须预留 Inspector 绑定字段，优先使用 `Image`、`TMP_Text`、`Slider`、`Button`、`Canvas`、`CanvasScaler`、`GraphicRaycaster`。
- Icon Button 必须拆成 `Button + Image background + Image icon + TMP_Text title/subtitle + redDot`，不要只用代码字符串模拟按钮。
- 进度条使用 `Slider`，资源、倒计时、状态展示使用 `TMP_Text`。
- 自动生成场景只作为默认样板；正式 UI 应允许设计师在 Prefab/Scene 中替换 Sprite、字体、布局并重新绑定字段。

## 脚本职责
- `MainSceneView` 只做页面 Presenter：编排子 Panel、事件订阅、按钮分发和进战斗。
- 子 Panel：`MainSceneProfilePanel`、`MainSceneResourcePanel`、`MainSceneCardPanel`、`MainSceneRewardPanel`。
- 通用 Widget：`GeneralCardPanel`、`UI_ItemSlot`、`GeneralRewardCardPanel`、`UI_RedDot`（见 `ui_architecture_workflow.md`）。
- 不在 UI 脚本中直接修改战斗对象，不直接实例化敌人/玩家/技能。
- 跨系统交互优先通过 `ServiceLocator` 获取 Manager，并通过 `GameEvents` 接收资源、签到、成就、商店状态变化。
- 高频文本不得每帧刷新；只在事件、显示、点击后刷新。

## 布局规范
- 竖屏移动端，Canvas Scaler 使用 `Scale With Screen Size`，参考分辨率 `1080 x 1920`。
- 重要入口区域：顶部账号/资源、左右活动入口、中部主视觉、底部奖励与开始按钮、底部导航。
- 所有可点击入口必须有明确 `MainSceneAction`，未接入功能也要输出可定位状态提示。

## 验收标准
- 打开 `MainScene` 后主界面能显示默认数据，资源/签到/红点可随事件刷新。
- 点击“开始战斗/战斗”能记录局内开始状态并进入 `BattleScene`。
- 点击签到、在线奖励、离线收益能调用现有 `DailyRewardManager` / `ShopManager`。
- 新增 UI 脚本前必须先读本 Prompt、`docs/version_02/prompt_ui_system.md` 与 `docs/version_02/ui_architecture_workflow.md`。
