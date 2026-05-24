

## 阶段四：UI 和游戏流程

### 模板 7：实现游戏 UI 和界面交互
目标
实现无限防守模式+肉鸽游戏的完整 UI 系统，包括 HUD、主菜单、暂停菜单、游戏结束界面和肉鸽升级选单。

上下文
使用 Unity UI（Canvas + Unity自带UI控件）

UI 数据绑定使用 GameEvents 响应游戏状态变化

移动端的Android和IOS以及小程序都适配（Canvas Scaler 使用 Scale With Screen Size）

肉鸽升级选单暂停游戏时间（Time.timeScale = 0）直到玩家选择

UI 交互音效通过 AudioManager.PlaySFX 触发

输出要求
生成 UIManager.cs：

引用所有 Canvas 面板（MainMenuPanel, GameplayHUD, PausePanel, GameOverPanel, UpgradePanel）

订阅 GameEvents 的状态变更，切换对应的面板

提供更新 HUD 数值的方法（UpdateGoldDisplay, UpdateScoreDisplay, UpdateHealthDisplay, UpdateWaveDisplay）

肉鸽升级选单打开和关闭时管理 Time.timeScale

生成 GameplayHUD 界面：

显示当前经验条、当前波次、玩家生命值

可选：显示当前激活的肉鸽 Buff 列表（悬浮小图标）

生成 UpgradePanelUI.cs：

继承自 UIManager 的升级面板逻辑

动态创建 3 个选择卡片实例（使用 Prefab）

卡片显示 BuffSO 的图标、名称、描述

选择后调用 UpgradeManager.SelectBuff()

生成 PausePanel 逻辑：

暂停按钮 触发暂停/恢复

显示 Resume、Restart、Main Menu 按钮

UI 布局
Canvas Scaler：UI Scale Mode = Scale With Screen Size，Reference Resolution = 1920x1080

GameplayHUD 放置在屏幕顶部和底部区域，中央区域为塔防玩法区

肉鸽升级选单居中显示，背景添加半透明遮挡层，升级选项以列表形式展示

使用 DoTween（可选）增强过渡动画：面板弹出、按钮悬停效果