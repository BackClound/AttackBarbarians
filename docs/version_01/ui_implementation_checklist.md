# UI 实现与视觉待办清单

> 对照 PRD、`work_flow.md` 第五阶段、`GameConstants.UiPanelIds` 与当前代码差距。  
> 设计师按 `prompt_ui_xianxia_visual_design.md` 出 Figma；程序按本清单落地 Unity。

---

## 一、当前工程状态（2025-05）

| 模块 | 代码状态 | UI 状态 |
|------|----------|---------|
| 伤害飘字 | `DamageNumberController` 已订阅事件 | 仅有 Prefab，无仙侠皮肤 |
| 玩家血条 | `PlayerHealthBarView` | 未接 UGUI 仙侠条 |
| 游戏状态/流程 | `GameFlowManager` 发 Panel 事件 | **无 UIManager**，面板未做 |
| 局内三选一 | 事件 `Upgrade.OptionsGenerated` 待接 | **未实现** |
| Meta 商城/抽奖/签到 | `MetaGameService`（PRD）/ 部分在需求文档 | **无 UI** |
| 天赋/装备 | `TalentManager` / `EquipmentManager` | **无成长面板** |
| 排行榜 | 未实现服务 | 仅 Wireframe |

---

## 二、你需要更新的 UI 细节（按优先级）

### P0 — 阻塞可玩闭环

1. **建立 `UIManager`**（`docs/prompt_ui_system.md`）
   - 订阅 `UI.PanelOpened` / `Closed`、`Game.StateChanged`
   - 映射 `ui.main_menu` / `ui.pause` / `ui.game_over` / `ui.wave_transition` / `ui.upgrade`

2. **GameplayHUD Prefab**
   - 顶：波次、计时、暂停
   - 条：HP、EXP（绑定 `Player.HealthChanged`、`Player.LevelUp`）
   - 底：技能槽 5、Buff 行 8
   - **中央留空**，Scaler 1080×1920

3. **UpgradePanel（三选一）**
   - `Time.timeScale=0` 与 `GameState.UpgradeChoosing` 同步
   - 卡片 Prefab：图标、名、描述、稀有度框（对齐 `ui_frame_rarity_*`）

4. **Pause / GameOver**
   - 与 `GameFlowManager` 状态一致；GameOver 展示结算字段（时长、波次、击杀、奖励）

5. **主菜单**
   - 普通/精英模式选择、体力显示、开始按钮
   - 体力不足弹窗

### P1 — Meta 与经济（PRD 一期）

6. **成长页 Tab**（属性 / 技能 / 天赋 / 装备）
   - 属性四维 + 卡消耗显示
   - 技能列表 + 锁定态（`total_play_seconds`）
   - 天赋树占位
   - 装备五槽 + 强化（订阅 `Equipment.Changed`）

7. **商城**
   - Tab、商品格、购买确认、货币不足
   - 12h 免费钻 Banner + 倒计时

8. **抽奖**
   - 单抽/十连、保底计数、**概率公示页**（合规 AC-13）
   - 扣费失败回滚提示

9. **签到**
   - 7 日格、第 7 日技能卡强调、已签印章、补签按钮

### P2 — 留存与社交

10. **排行榜**（可先本地假数据）
11. **成就**列表 + 领取
12. **设置**（音量、协议、版本）
13. **离线巡逻**领取弹窗（8h 封顶文案）

### P3 — 视觉打磨

14. 全局仙侠组件库替换占位图
15. DoTween 面板进出场（`UI和游戏流程.md`）
16. `AudioManager.PlaySFX` 绑按钮
17. 红点：`SignIn` / `FreeDiamond` / `Achievement` 可领

---

## 三、代码侧需同步扩展

### `GameConstants.UiPanelIds` 建议新增

```csharp
public const string Shop = "ui.shop";
public const string Gacha = "ui.gacha";
public const string SignIn = "ui.sign_in";
public const string Leaderboard = "ui.leaderboard";
public const string Growth = "ui.growth";
public const string Achievement = "ui.achievement";
public const string Settings = "ui.settings";
public const string StaminaEmpty = "ui.stamina_empty";
public const string OfflinePatrol = "ui.offline_patrol";
```

### 建议目录结构

```
Assets/UI/
  Prefabs/Panels/
  Prefabs/Widgets/
  Sprites/Icons/   ← 按 ui_icon_export_manifest.md
  Fonts/
  Animations/
```

### 与 PRD 冲突需产品确认

| 项 | PRD | 当前工程 |
|----|-----|----------|
| 装备 | 需求文档写「二期」 | 代码已实现 `EquipmentManager` |
| Reference Resolution | `UI和游戏流程.md` 写 1920×1080 | 项目定位竖屏，建议 **1080×1920** |
| 波次 vs 时间轴 | PRD 强调时间轴 Boss | `GameFlowManager` 用 Wave 状态 |

---

## 四、Figma 交付物检查（设计师）

- [ ] Figma 文件含 11 个 Page（见 `ui_wireframe_figma_spec.md`）
- [ ] `00_DesignSystem` 组件与 Variables 齐全
- [ ] 每页至少 1 个 Happy Path + 1 个 Empty/Error 态
- [ ] `99_Export_Slices` 导出 ≥88 图标
- [ ] 九宫格 4 张面板底图已标 Border
- [ ] 标注：字号、色值、间距、点击热区 ≥88px
- [ ] Dev Mode 链接或 PDF 标注稿交付程序

---

## 五、测试步骤（UI 完成后）

1. 主菜单 → 普通模式 → HUD 显示 → 暂停 → 恢复
2. 升级事件 → 三选一 → 选后战斗恢复、计数不重置
3. 死亡 → 结算 → 返回主菜单
4. 商城购买成功/失败提示
5. 抽奖扣费 → 结果 → 存档 `gachaPityCounter` 更新
6. 签到同日不可重复；第 7 日奖励类型正确
7. 竖屏小屏机（720×1280）无按钮遮挡

---

## 六、文档索引

| 文档 | 用途 |
|------|------|
| `prompt_ui_xianxia_visual_design.md` | AI/Figma 视觉 Prompt |
| `ui_wireframe_figma_spec.md` | 页面线框与尺寸 |
| `ui_icon_export_manifest.md` | 切图命名与目录 |
| `figma/figma_project_structure.json` | Figma 页面树导入参考 |
| `prompt_ui_system.md` | 程序 UIManager 需求 |
