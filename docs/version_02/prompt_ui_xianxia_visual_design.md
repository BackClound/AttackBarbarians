# UI 视觉设计 Prompt（仙侠风 · 无限防守 + Roguelike）

> **版本 v2** | 架构层级见 `architecture_design.md` | 工作流见 `.cursor/rules/work_flow.md`

## v2 架构约束（必读）
- **分层依赖**：本模块所属层级不得反向引用高层模块（见 `architecture_design.md` §2）。
- **属性唯一真相源**：`PlayerRuntimeStats` → `ConfigStatBridge` → `Entity_Stats`；禁止直接修改 Entity_Stats 做 Buff。
- **Player 查找**：统一使用 `PlayerSceneAccess`，禁止 `FindObjectOfType<Player*>`。
- **跨模块通信**：优先 `GameEvents`；Manager 之间通过 `ServiceLocator` 或事件，不直接持有场景实体。
- **对象池**：敌人/子弹/特效/飘字必须走 `PoolManager`。
- **Legacy 禁止扩展**：不新增对 `EnemyGenerateManager`、`PlayerCombat`、`EnemyCombatManager` 的依赖。

# UI 视觉设计 Prompt（仙侠风 · 无限防守 + Roguelike）

> 用途：在 **Figma AI / Midjourney / SD / 即梦** 中生成高保真稿；在 **Figma** 中建立组件库与页面 Frame。  
> 关联：`docs/ui_wireframe_figma_spec.md`（页面线框与尺寸）、`docs/ui_icon_export_manifest.md`（切图命名）、`docs/figma/figma_project_structure.json`（Figma 页面树）。

---

## 1. 全局风格 Prompt（复制到 AI 图像工具）

```
Mobile game UI, vertical portrait 1080x1920, Chinese Xianxia fantasy style,
ink-wash gradient background, jade green and cinnabar red accent, gold filigree borders,
cloud mist overlays, talisman paper texture panels, semi-transparent dark indigo scrims,
elegant serif Chinese titles, clean sans-serif body text,
rounded scroll-frame buttons, glowing spiritual energy progress bars,
subtle particle sparkles, NOT cartoon chibi, NOT western medieval,
high readability for HUD numbers, safe area margins 48px top/bottom,
game-ready flat UI layers separated from gameplay center void,
professional commercial mobile game quality, 2D UI mockup only
```

**负面 Prompt：**
```
low resolution, blurry text, illegible numbers, cluttered center play area,
western RPG UI, cyberpunk, photorealistic character blocking UI,
excessive ornament covering buttons, dark text on dark background
```

---

## 2. 设计 Token（Figma Variables 建议）

| Token | 值 | 用途 |
|-------|-----|------|
| `color/bg/deep` | `#0B1020` | 全屏底 |
| `color/bg/panel` | `#1A2338` @ 88% | 面板 |
| `color/bg/scrim` | `#000000` @ 55% | 弹层遮罩 |
| `color/primary` | `#3D8F7A` | 主色（玉青） |
| `color/accent` | `#C45C48` | 强调（丹朱） |
| `color/gold` | `#D4AF37` | 稀有/货币高亮 |
| `color/text/primary` | `#F5F0E6` | 主文案 |
| `color/text/secondary` | `#B8C4D0` | 副文案 |
| `color/hp` | `#E85D5D` | 生命 |
| `color/exp` | `#5BC0EB` | 经验/灵力 |
| `radius/lg` | `24` | 大面板 |
| `radius/md` | `16` | 卡片 |
| `radius/sm` | `8` | 图标底 |
| `space/base` | `8` | 栅格单位 |
| `font/title` | 思源宋体 / 方正清刻本悦宋 | 标题 |
| `font/body` | 思源黑体 / 阿里巴巴普惠体 | 正文 |

---

## 3. Figma 项目搭建 Prompt（给设计师 / Figma AI）

```
Create a Figma file "AttackBarbarians_UI_Xianxia" with pages:
00_DesignSystem, 01_MainMenu, 02_Battle_HUD, 03_Roguelike_Flow,
04_Meta_Growth, 05_Shop, 06_Gacha, 07_SignIn, 08_Leaderboard,
09_Achievement, 10_Settings, 99_Export_Slices.

On 00_DesignSystem build:
- Color styles from token table (jade, cinnabar, gold, ink background)
- Text styles: H1 48px, H2 36px, Body 28px, Caption 22px (mobile scale)
- Components: Btn/Primary, Btn/Secondary, Btn/Icon, Panel/Scroll, Panel/Modal,
  Bar/HP, Bar/EXP, Bar/Stamina, Chip/Currency, Card/BuffChoice, Card/ShopItem,
  Tab/BottomNav, IconFrame/Circle, Badge/RedDot, Toast, Tooltip

Each screen frame: 1080 x 1920, layout grid 8px, constraints for top/bottom HUD.

Style: Chinese Xianxia mobile game UI, jade and gold borders, cloud motifs,
talisman corners on panels, gameplay area center 60% height kept empty in battle screens.

Export page 99: all icons @1x @2x PNG, slices named per ui_icon_export_manifest.md
```

---

## 4. 分页面生成 Prompt

### 4.1 首页 / 主菜单 `01_MainMenu`

```
Xianxia mobile main menu, vertical 1080x1920,
top: player avatar in circular jade frame, nickname, combat power number,
stamina bar with moon icon, currencies gold and diamond top-right,
center: large title "御妖守关" with golden seal logo,
mode cards: Normal mode and Elite mode with red elite border glow,
big jade button "开始修行", bottom tab bar 5 icons: Home Battle Shop Rank More,
left floating: Sign-in red dot, right floating: Gacha sparkle,
background: misty mountain night, pagoda silhouette, subtle animated cloud parallax hint
```

### 4.2 战斗 HUD `02_Battle_HUD`

```
In-game HUD overlay only, center empty for gameplay,
top bar: wave "第12波", timer 05:32, pause gear icon,
player HP bar red with spirit pattern, EXP bar blue below,
bottom: 5 skill slots cooldown radial masks, buff icon row max 8 small squares,
left mini: gold + kill count, right mini: DPS placeholder,
semi-transparent, does not block center 55% vertical space,
xianxia jade frame on bars, small talisman on pause button
```

### 4.3 局内 Roguelike 三选一 `03_Roguelike_Flow`

```
Full screen dim scrim, central title "天道择一",
three vertical scroll cards equal width, each: rarity border color
(common gray, rare blue, epic purple, legendary gold),
icon 128px, name, 2-line description, tag chips "攻击+15%",
selected card glow gold, confirm button bottom,
time frozen indicator subtle clock-stop icon
```

### 4.4 波次过渡 `ui.wave_transition`

```
Banner top "第13波 来袭", enemy preview silhouettes 3 icons,
countdown 3-2-1 large numbers, reward preview +gold,
continue button "迎敌"
```

### 4.5 暂停 / 结算

**Pause:** 半透明遮罩，竖排按钮：继续修行 / 重开本局 / 返回山门（主菜单）。  
**GameOver:** 卷轴面板，本局时长、击杀、波次、获得金币/卡；按钮：再次挑战 / 返回山门 / 分享占位。

### 4.6 角色成长（属性 + 技能 + 天赋 + 装备）`04_Meta_Growth`

```
Tab header: 属性 | 技能 | 天赋 | 装备,
属性页: 4 big stat tiles HP ATK MoveSpeed AttackSpeed each with level and + button,
cost chip "消耗 3 张属性卡", skill list scroll with lock overlay if not unlocked,
talent tree light vertical path nodes, equipment mannequin center + 5 slots weapon head chest boots accessory,
enhance button gold, set bonus 2-piece text green
```

### 4.7 商城 `05_Shop`

```
Top tabs: 推荐 | 资源 | 礼包 | 免费,
12h free diamond banner with countdown timer,
grid 2 columns product cards: icon, name, price diamond/gold, limit "每日1/1",
purchase button jade, insufficient currency grayed
```

### 4.8 抽奖 `06_Gacha`

```
Gacha page xianxia treasure pavilion theme,
center lotus platform, single draw and ten-draw buttons,
currency diamond and ticket counters, pity counter "再抽 8 次必出史诗",
probability disclosure link bottom (合规),
result overlay 1 or 10 item reveals with flip animation hint
```

### 4.9 签到 `07_SignIn`

```
7-day calendar horizontal, day 1-6 small attribute card icons, day 7 large skill card,
today highlighted gold border, claimed days seal stamp "已签",
claim button, makeup sign gray secondary button,
cycle text "第2轮 第3天"
```

### 4.10 排行榜 `08_Leaderboard`

```
Tabs: 波次最高 | 单局时长 | 战力,
top3 podium avatars with rank medals 1 gold 2 silver 3 bronze,
scroll list rank number avatar name score, self row sticky bottom highlighted jade,
refresh icon, season end timer caption
```

### 4.11 成就 `09_Achievement`

```
Category chips: 战斗 成长 收集 社交,
achievement rows: icon, title, progress 45/100 bar, reward preview, claim button,
completed rows faded with check seal
```

### 4.12 设置 / 通用

```
Settings: BGM/SFX sliders, vibration toggle, language, privacy, account,
confirm dialog component: title, body, cancel/ok jade buttons
```

---

## 5. 与代码对齐的 Panel ID

| Figma Frame | `GameConstants.UiPanelIds` / 逻辑 | 状态 |
|-------------|-----------------------------------|------|
| MainMenu | `ui.main_menu` | 已定义 |
| GameplayHUD | （常显，非 Panel 事件） | 待实现 |
| Pause | `ui.pause` | 已定义 |
| GameOver | `ui.game_over` | 已定义 |
| WaveTransition | `ui.wave_transition` | 已定义 |
| Upgrade | `ui.upgrade` | 已定义 |
| Shop / Gacha / SignIn / Leaderboard / Growth | 需在 `UiPanelIds` 扩展 | 待实现 |

---

## 6. 动效建议（Unity DoTween / Animator）

| 交互 | 动效 |
|------|------|
| 面板打开 | Scale 0.92→1 + Alpha 0→1，200ms OutQuad |
| 三选一卡片 | Stagger 上移 24px + 淡入 |
| 按钮按下 | Scale 0.96，80ms |
| 红点 | 呼吸 Scale 1→1.08 循环 |
| 抽奖结果 | 卡牌翻转 Y 0→180° |

---

## 7. 验收清单（视觉 + 工程）

- [ ] 竖屏 1080×1920 安全区无关键按钮被刘海遮挡
- [ ] 战斗中央 55% 高度无 UI 遮挡操作区
- [ ] 所有可点击区域 ≥ 88×88 px（@1x）
- [ ] 稀有度颜色与 `UpgradeOptionSO` / 装备品质一致
- [ ] 抽奖页含概率公示入口（PRD AC-13）
- [ ] Icon 命名与 `ui_icon_export_manifest.md` 一致
- [ ] 导出 @2x PNG 已放入 `Assets/UI/Sprites/` 对应目录

## v2 验收标准补充
- 完成后须验证与 `architecture_design.md` 中本模块的数据流、事件流一致。
- 列出受影响脚本、Prefab/Scene 挂载、ContextMenu 或手动测试步骤。
- 标注仍依赖的 Legacy 代码及后续清理计划。
- 新类推荐 namespace：`AttackBarbarians.{Layer}.{Module}`（迁移期可与全局类并存）。

## v2 模块依赖边界
- 仅依赖 architecture_design.md 中本层及以下层的公共接口、配置 SO、Event Key。
- 禁止从低层模块反向引用 UI、Shop、Upgrade 等 unless 本模块即为该层。
- Event Payload 保持小而稳定；禁止暴露 Manager 内部可变状态。
