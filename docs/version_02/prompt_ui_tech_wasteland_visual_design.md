# UI 视觉设计 Prompt（科技废土风 · 无限防守 + Roguelike）

> **版本 v2** | 架构层级见 `architecture_design.md` | 工作流见 `.cursor/rules/work_flow.md`

## v2 架构约束（必读）
- **分层依赖**：本模块所属层级不得反向引用高层模块（见 `architecture_design.md` §2）。
- **属性唯一真相源**：`PlayerRuntimeStats` → `ConfigStatBridge` → `Entity_Stats`；禁止直接修改 Entity_Stats 做 Buff。
- **Player 查找**：统一使用 `PlayerSceneAccess`，禁止 `FindObjectOfType<Player*>`。
- **跨模块通信**：优先 `GameEvents`；Manager 之间通过 `ServiceLocator` 或事件，不直接持有场景实体。
- **对象池**：敌人/子弹/特效/飘字必须走 `PoolManager`。
- **Legacy 禁止扩展**：不新增对 `EnemyGenerateManager`、`PlayerCombat`、`EnemyCombatManager` 的依赖。

# UI 视觉设计 Prompt（科技废土风 · 无限防守 + Roguelike）

> 用途：在 **Figma AI / Midjourney / SD / 即梦 / Flux** 中生成高保真稿；在 **Figma** 中建立组件库与页面 Frame。  
> 页面结构、尺寸、Panel ID 与仙侠版相同，见 `docs/ui_wireframe_figma_spec.md`。  
> Icon 命名仍用 `docs/ui_icon_export_manifest.md`（仅视觉造型改为废土科技风）。  
> Figma 页面树：`docs/figma/figma_project_structure_tech_wasteland.json`

---

## 0. 风格定义（给设计师一句话）

**科技废土（Tech Wasteland）**：文明崩塌后的近未来废土，UI 像「用战前终端 + 回收金属 + 全息投影勉强拼起来的生存界面」——脏、硬、可读、带轻微故障感，但不影响数值识别。

**参考气质：** 《辐射》PIP-Boy 信息密度 + 《死亡搁浅》工业简洁 + 《尼尔》故障美学 + 国产手游竖屏商业化排版。

**避免：** 仙侠卷轴/水墨/玉佩；过度赛博霓虹夜店风；纯黑白看不清稀有度；中央玩法区被 UI 占满。

---

## 1. 全局风格 Prompt（复制到 AI 图像工具）

### 1.1 主 Prompt

```
Mobile game UI mockup, vertical portrait 1080x1920, tech wasteland post-apocalyptic sci-fi style,
rusted metal panels, welded steel borders, hazard yellow black diagonal stripes accents,
CRT terminal green and amber holographic UI elements, subtle scanlines and glitch artifacts,
concrete and sand dust textures, exposed bolts and rivets, duct tape patches on corners,
dark desaturated background #1a1c1f with distant ruined city silhouette,
cyan teal primary accent for interactive elements, amber orange for warnings and elite mode,
cold white monospace HUD numbers, clean sans-serif Chinese labels,
rectangular industrial buttons with chamfered corners, progress bars like fuel gauges,
semi-transparent soot scrim overlays, tactical survival HUD layout,
center 55% vertical gameplay void kept empty on battle screens,
high contrast readable currency and HP numbers, safe area 48px top bottom,
professional commercial mobile game UI, flat 2D layers, no character art blocking UI
```

### 1.2 负面 Prompt

```
xianxia, fantasy scroll, jade, gold filigree, ink wash, temple, lotus, talisman,
clean sterile apple UI, bubbly cartoon, chibi, excessive neon cyberpunk city,
low resolution, blurry text, illegible numbers, cluttered center play area,
photorealistic 3D character portrait covering buttons, ornate decoration blocking taps,
pure black on black text, rainbow gradient overload
```

### 1.3 风格关键词速查（可追加到分屏 Prompt 末尾）

`rusted metal` · `welded frame` · `hologram flicker` · `CRT phosphor` · `hazard stripe` · `survival terminal` · `scrap icon` · `dust vignette` · `radio static noise texture`

---

## 2. 设计 Token（Figma Variables）

| Token | 值 | 用途 |
|-------|-----|------|
| `color/bg/void` | `#121416` | 全屏底（虚空黑） |
| `color/bg/panel` | `#1E2228` @ 92% | 主面板（钢板） |
| `color/bg/panel/rust` | `#2A241C` @ 90% | 锈蚀强调面板 |
| `color/bg/scrim` | `#000000` @ 62% | 弹层遮罩 |
| `color/primary` | `#2EC4B6` | 主交互（全息青） |
| `color/primary/dim` | `#1A8A80` | 主色按下态 |
| `color/accent` | `#FF9F1C` | 强调 / 精英 / 警告（琥珀） |
| `color/accent/danger` | `#E63946` | 危险 / 低血量闪烁 |
| `color/loot/rare` | `#4CC9F0` | 稀有 |
| `color/loot/epic` | `#9B5DE5` | 史诗 |
| `color/loot/legend` | `#F4D35E` | 传说（战前金漆剥落感） |
| `color/text/primary` | `#E8EDF2` | 主文案 |
| `color/text/secondary` | `#8B9AAB` | 副文案 |
| `color/text/terminal` | `#65FF9A` | 终端读数（可选） |
| `color/hp` | `#E63946` | 生命条 |
| `color/exp` | `#2EC4B6` | 经验 / 能量条 |
| `color/hazard` | `#FFBE0B` | 警示条带 |
| `border/hard` | `#3D4450` | 1px 硬边 |
| `border/glow` | `#2EC4B6` @ 40% | 选中外发光 |
| `radius/chamfer` | `4` | 切角矩形（少用圆角） |
| `radius/md` | `8` | 卡片 |
| `space/base` | `8` | 栅格 |
| `font/title` | 思源黑体 Heavy / Orbitron（英文装饰） | 标题 |
| `font/body` | 思源黑体 Regular | 正文 |
| `font/hud` | Roboto Mono / IBM Plex Mono | 数字 / 计时 / 波次 |
| `fx/scanline` | 4% 水平线叠加 | 面板纹理 |
| `fx/glitch` | 1–2px 位移，仅选中/稀有 | 克制使用 |

### 稀有度边框（与玩法一致）

| 稀有度 | 边框色 | 装饰 |
|--------|--------|------|
| Common | `#5C6670` | 素铁板 |
| Rare | `#4CC9F0` | 细全息线 |
| Epic | `#9B5DE5` | 角标灯带 |
| Legendary | `#F4D35E` | 剥落金边 + 轻微 glitch |

---

## 3. Figma 项目搭建 Prompt

```
Create a Figma file "AttackBarbarians_UI_TechWasteland" with pages:
00_DesignSystem, 01_MainMenu, 02_Battle_HUD, 03_Roguelike_Flow,
04_Meta_Growth, 05_Shop, 06_Gacha, 07_SignIn, 08_Leaderboard,
09_Achievement, 10_Settings, 99_Export_Slices.

00_DesignSystem:
- Color styles: void black, steel panel, hologram cyan, amber warning, hazard yellow,
  terminal green, rarity rare/epic/legend
- Text: H1 48px heavy, H2 36px, Body 28px, Caption 22px, HUD Mono 32px for numbers
- Components: Btn/Primary (chamfered steel), Btn/Secondary, Btn/Danger,
  Panel/MetalPlate, Panel/Terminal, Panel/Modal, Bar/HP (gauge), Bar/EXP (energy),
  Bar/Stamina (battery), Chip/Currency, Card/BuffModule, Card/ShopCrate,
  Tab/BottomNav (5), IconFrame/Hex, Badge/AlertDot, Toast, Tooltip,
  Decor/HazardStripe, Decor/Rivet, Decor/ScanlineOverlay

All frames 1080x1920, 8px grid, battle screens keep center 55% height empty.

Visual language: post-apocalyptic survival terminal UI, rust and welded metal,
NOT xianxia, NOT bright cyberpunk nightclub. Icons: flat with 2px inner stroke,
scrap metal and hologram mix.

Export 99_Export_Slices: PNG @2x, names from ui_icon_export_manifest.md
```

---

## 4. 分页面生成 Prompt

### 4.1 首页 / 主菜单 `01_MainMenu`

```
Tech wasteland mobile main menu, vertical 1080x1920,
top HUD strip: survivor ID avatar in hexagonal metal frame, callsign nickname,
combat power "PWR 12840" in monospace, stamina as battery segment bar,
scrap gold and quantum diamond currency chips top-right,
center logo: game title in Chinese "壁垒前线" or "废土守线" with cracked hologram logo,
two mode deploy cards side by side: STANDARD (steel blue) and ELITE (amber hazard border, radiation icon),
large primary deploy button "启动防线" with cyan edge glow,
bottom command bar 5 icons: BASE OPS SHOP RANK SYS (home battle shop rank more),
left float: daily login crate with red alert dot, right float: gacha supply drop pod icon,
background: ruined overpass, sandstorm, distant broken megastructure, parallax dust particles,
color palette dark steel + cyan + amber, readable Chinese labels
```

### 4.2 战斗 HUD `02_Battle_HUD`

```
In-game survival HUD overlay, tech wasteland, center empty for tower defense gameplay,
top-left: WAVE-12 in terminal font, top-center timer 05:32 with blinking colon,
top-right pause icon as power switch symbol 88px,
HP bar horizontal gauge red segment with metal casing, numeric HP overlay,
EXP bar below cyan energy fill like reactor charge,
bottom row 5 ability module slots 96px with cooldown radial wipe and hex borders,
buff module row up to 8 small square chips with status LEDs,
bottom-left scrap gold counter, bottom-right kill count,
semi-transparent soot scrim on bars only, NO UI in center 55% height,
subtle scanline on top bar, small glitch on wave number when boss incoming optional
```

### 4.3 局内 Roguelike 三选一 `03_Roguelike_Flow` · `ui.upgrade`

```
Full screen dark scrim 62%, title center "模块重组 // SELECT 1",
three equal vertical cards like loot modules on magnetic rails,
each card: rarity metal frame (gray cyan purple gold), icon 128px sci-fi skill chip,
module name, 2-line description, stat tags "+ATK 15%" in terminal chips,
selected card: cyan outer glow + corner hazard marks, confirm button "确认装载" bottom,
top-right small icon SYSTEM PAUSED time freeze, faint static noise texture
```

### 4.4 波次过渡 `ui.wave_transition`

```
Top banner "WAVE 13 INBOUND" with amber alert stripe,
three enemy silhouette scans in targeting box style,
large countdown 3-2-1 monospace with screen flash frame,
reward preview scrap +gold icon,
primary button "接敌" deploy style, radar sweep line animation hint
```

### 4.5 暂停 `ui.pause` / 结算 `ui.game_over`

**Pause**
```
Soot scrim overlay, central metal dialog plate,
title "战术暂停", vertical buttons:
继续作战 / 重启本局 / 返回基地,
buttons chamfered steel, primary cyan highlight on resume
```

**GameOver**
```
Settlement terminal panel 920x1200, header "任务终止 // REPORT",
rows with mono labels: 存活时长, 最高波次, 击杀数, 获得资源,
reward row icons scrap cards, buttons 再次部署 / 返回基地 / 分享(占位),
cracked glass texture on panel edge, amber stamp "FAILED" or green "EXTRACTED" for clear
```

### 4.6 角色成长 `04_Meta_Growth`

```
Meta upgrade screen tech wasteland,
tab bar: 属性 | 技能 | 天赋 | 装备 as metal tabs with underline LED,
属性页: 4 stat modules HP ATK SPD ASPD in 2x2 grid, each like implant card with level and UP button,
cost label "消耗 属性模组 x3", skill list rows with lock icon and playtime requirement text,
talent tree vertical PCB trace style nodes connected by cable lines,
equipment page: exosuit mannequin wireframe center, 5 slots weapon head chest boots module,
enhance button "强化" amber, set bonus "套装[2] 已激活" cyan terminal text
```

### 4.7 商城 `05_Shop`

```
Wasteland supply shop UI,
tabs 推荐 | 资源 | 礼包 | 免费 as folder tabs,
top banner FREE QUANTUM +5 every 12h with countdown timer bar,
2-column product crates: item icon, name, price diamond or scrap, limit "1/1日",
buy button cyan "购买", sold out gray with stencil SOLD OUT,
background shelves with dim floodlight, subtle dust
```

### 4.8 抽奖 `06_Gacha`

```
Gacha supply drop chamber theme, not fantasy chest,
center vertical drop pod bay with hologram ring,
buttons 单抽 / 十连 with ticket and diamond cost,
pity counter "保底 8/10" progress rail epic purple,
bottom link "概率公示" terminal style compliance,
result overlay: 1 or 10 item cards eject from pod with light streak,
rare epic legend frames per token colors
```

### 4.9 签到 `07_SignIn`

```
7-day supply calendar horizontal,
day 1-6 small attribute module icons in metal cells,
day 7 large skill chip cell highlighted amber,
today cell cyan border pulse, claimed cells stamped "RECEIVED" stencil,
claim button primary, makeup sign secondary gray "补签",
subtitle "周期 2 // 第 3 天"
```

### 4.10 排行榜 `08_Leaderboard`

```
Leaderboard terminal,
tabs 波次 | 时长 | 战力 with LED indicator,
top3 podium: rank 1 gold plate 2 silver 3 bronze with survivor avatars,
list rows: rank mono, avatar, name, score right aligned,
self row sticky bottom highlighted cyan bar "你的排名",
refresh icon circular arrow, season timer small caption
```

### 4.11 成就 `09_Achievement`

```
Achievement log database UI,
category filters 战斗 成长 收集 社交 as toggle chips,
rows: badge icon, title, progress bar 45/100 with segment blocks,
reward preview icons, claim button "领取",
completed rows dimmed with green check terminal icon
```

### 4.12 设置 / 通用 `10_Settings`

```
Settings control panel,
sliders BGM / SFX with metal track, vibration toggle switch industrial style,
language privacy account rows,
confirm dialog: title, body, 取消 / 确认 on steel buttons,
version string bottom mono "BUILD 0.3.0"
```

---

## 5. Icon 造型 Prompt（批量生成图标时用）

```
Set of mobile game UI icons, tech wasteland style, flat design, 2px inner stroke,
scrap metal plate background optional, hologram cyan accent lines,
readable at 64px, transparent PNG, consistent 45 degree light from top-left,
subjects: currency gold scrap diamond quantum, stamina battery, settings gear wrench,
pause power, shop crate, rank trophy antenna, skills as microchip modules,
NO xianxia symbols, NO cute emoji, vector-like clarity
```

**分类追加词：**

| 类型 | 追加 Prompt |
|------|-------------|
| 货币 | `crushed gold ingot`, `blue quantum crystal`, `gacha ticket barcode` |
| 技能 | `microchip`, `lightning coil`, `fire bombardment crosshair` |
| 装备 | `welded armor plate`, `rusty axe`, `gas mask silhouette` |
| 导航 | `bunker door`, `crosshair target`, `supply crate`, `antenna tower` |

---

## 6. 组件结构规范（与 Unity Prefab 对应）

| 组件 | 废土造型要点 |
|------|----------------|
| `Btn/Primary` | 钢板底 + 左上和右下切角 4px + 按下内阴影 |
| `Btn/Danger` | 琥珀底 + 黑色警示条纹左缘 8px |
| `Panel/MetalPlate` | 铆钉四角 + 1px 锈边 + 可选 scanline 叠加层 8% |
| `Panel/Terminal` | 纯黑内屏区 + 绿色/青色 1px 内描边 + 等宽字体区 |
| `Bar/HP` | 分段金属槽 + 红色液体填充 + 低血量琥珀闪烁 |
| `Bar/EXP` | 反应堆能量条，填充带轻微横向流动纹理 |
| `Card/BuffModule` | 磁轨卡片 + 稀有度灯带顶边 4px |
| `Chip/Currency` | 胶囊形，左图标右数字，mono 数字 |
| `Badge/AlertDot` | 方形红灯 `#E63946`，非圆润红点 |

---

## 7. 文案语气（中文化废土）

| 场景 | 推荐文案 | 避免 |
|------|----------|------|
| 开始 | 启动防线 / 部署 | 开始修行 |
| 暂停 | 战术暂停 | 闭关 |
| 三选一 | 模块重组 | 天道择一 |
| 返回 | 返回基地 | 返回山门 |
| 结算 | 任务报告 | 卷轴结算 |
| 商城 | 补给站 | 宝阁 |
| 抽奖 | 空投补给 | 天机阁 |

> 标题可中英混排：`WAVE 12` + `第12波`，增强终端感；关键按钮保持纯中文易读。

---

## 8. 与代码对齐（不变）

| Figma Frame | Panel ID |
|-------------|----------|
| MainMenu | `ui.main_menu` |
| GameplayHUD | 常显 |
| Pause | `ui.pause` |
| GameOver | `ui.game_over` |
| WaveTransition | `ui.wave_transition` |
| Upgrade | `ui.upgrade` |
| Shop / Gacha / SignIn / Leaderboard / Growth | 待扩展，见 `ui_implementation_checklist.md` |

Canvas：**1080×1920** 竖屏，`Scale With Screen Size`，Match 0.5。

---

## 9. 动效建议（废土感）

| 交互 | 动效 |
|------|------|
| 面板打开 | 自下而上 slide 16px + Alpha 0→1，180ms；无弹性回弹 |
| 三选一卡片 | 依次 fade + 1px glitch 位移 60ms stagger |
| 按钮按下 | 内阴影加深 + 轻微金属压陷 1px |
| 警告/精英 | 琥珀色 2Hz 慢闪边框 |
| 抽奖结果 | 卡片从下方弹仓滑出 + 顶部探照灯扫过 |
| 终端数字 | 可选 0.3s 数字滚动（仅结算大数字） |

---

## 10. 验收清单

- [ ] 全程无仙侠元素残留（卷轴、玉佩、水墨山）
- [ ] 竖屏 1080×1920，战斗中央 55% 无遮挡
- [ ] 可点击热区 ≥ 88×88 px
- [ ] HUD 数字在沙尘底上仍可辨认（对比度 ≥ 4.5:1）
- [ ] 稀有度色与 `UpgradeOptionSO` / `EquipmentQuality` 一致
- [ ] 抽奖页含「概率公示」入口
- [ ] Icon 导出命名仍遵循 `ui_icon_export_manifest.md`
- [ ] 故障/扫描线效果不影响色弱用户识别稀有度边框

---

## 11. 一键复制：Master Prompt（整包出图）

将下列整段用于「一次生成主菜单+HUD 拼图」或作为 Custom GPT / Figma AI 系统提示：

```
You are designing a complete mobile game UI kit for "Attack Barbarians",
a vertical 1080x1920 infinite defense roguelike in TECH WASTELAND style.

Rules:
- Post-apocalyptic survival interface: rusted metal, welded panels, hazard stripes,
  hologram cyan (#2EC4B6) and amber (#FF9F1C) accents, dark void background.
- Use Chinese labels for player-facing buttons; mono font for numbers and wave/timer.
- Battle HUD must keep center 55% empty. Meta screens: main menu, upgrade 3-choice,
  shop, gacha, 7-day sign-in, leaderboard, growth tabs, settings.
- Rarity colors: common gray, rare cyan, epic purple, legendary worn gold.
- No xianxia, no fantasy scrolls, no neon cyberpunk city.
- Output clean layered 2D UI mockup suitable for Figma trace.

Deliver frames listed in docs/ui_wireframe_figma_spec.md with wasteland visual treatment.
```

## v2 验收标准补充
- 完成后须验证与 `architecture_design.md` 中本模块的数据流、事件流一致。
- 列出受影响脚本、Prefab/Scene 挂载、ContextMenu 或手动测试步骤。
- 标注仍依赖的 Legacy 代码及后续清理计划。
- 新类推荐 namespace：`AttackBarbarians.{Layer}.{Module}`（迁移期可与全局类并存）。

## v2 模块依赖边界
- 仅依赖 architecture_design.md 中本层及以下层的公共接口、配置 SO、Event Key。
- 禁止从低层模块反向引用 UI、Shop、Upgrade 等 unless 本模块即为该层。
- Event Payload 保持小而稳定；禁止暴露 Manager 内部可变状态。
