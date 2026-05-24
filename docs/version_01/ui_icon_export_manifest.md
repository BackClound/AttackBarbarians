# UI Icon 切图导出清单（Figma → Unity）

> 在 Figma `99_Export_Slices` 页面统一导出。格式：**PNG**（透明底），@1x 基准以 **1080 宽** 设计稿为准，Unity 导入 **@2x**（2160 设计稿导出或 Figma 2x）。  
> **造型风格：** 科技废土见 `prompt_ui_tech_wasteland_visual_design.md` §5；文件名不变。

---

## 导出规范

| 项 | 值 |
|----|-----|
| 格式 | PNG-24，透明 |
| 倍率 | @1x + @2x（或仅 @2x，Unity PPU=100） |
| 命名 | `snake_case`，前缀表见下 |
| Unity 目录 | `Assets/UI/Sprites/Icons/{category}/` |
| Sprite Mode | Single；UI 用 Mesh Type Full Rect |
| 九宫格 | 仅面板/按钮底图，见「可拉伸资源」 |

---

## 命名前缀

| 前缀 | 含义 |
|------|------|
| `ui_icon_` | 功能图标 |
| `ui_curr_` | 货币 |
| `ui_attr_` | 四维属性 |
| `ui_skill_` | 技能 |
| `ui_buff_` | Buff/升级选项 |
| `ui_equip_` | 装备部位/品质框 |
| `ui_nav_` | 底栏导航 |
| `ui_frame_` | 品质框/头像框 |
| `ui_panel_` | 可拉伸面板底 |

---

## A. 导航与系统（@1x 推荐尺寸）

| 文件名 | 尺寸 | 页面 | 说明 |
|--------|------|------|------|
| `ui_nav_home` | 96×96 | 主菜单 | 山门 |
| `ui_nav_battle` | 96×96 | 主菜单 | 历练 |
| `ui_nav_shop` | 96×96 | 商城 | 宝阁 |
| `ui_nav_rank` | 96×96 | 排行 | 榜 |
| `ui_nav_more` | 96×96 | 更多 | 设置入口 |
| `ui_icon_settings` | 88×88 | 设置 | 齿轮→云纹齿 |
| `ui_icon_pause` | 88×88 | HUD | 暂停符箓 |
| `ui_icon_close` | 64×64 | 弹窗 | 关闭 |
| `ui_icon_back` | 64×64 | 子页 | 返回 |
| `ui_icon_share` | 64×64 | 结算 | 分享占位 |
| `ui_icon_refresh` | 64×64 | 排行 | 刷新 |
| `ui_icon_info` | 48×48 | 抽奖 | 概率说明 |
| `ui_badge_red_dot` | 24×24 | 全局 | 红点 |

---

## B. 货币与资源

| 文件名 | 尺寸 |
|--------|------|
| `ui_curr_gold` | 64×64 |
| `ui_curr_diamond` | 64×64 |
| `ui_curr_ticket_gacha` | 64×64 |
| `ui_curr_stamina` | 64×64 |
| `ui_curr_energy` | 64×64 |
| `ui_curr_card_attr` | 80×80 |
| `ui_curr_card_skill` | 80×80 |
| `ui_curr_card_skill_universal` | 80×80 |

---

## C. 属性四维

| 文件名 | 尺寸 |
|--------|------|
| `ui_attr_hp` | 96×96 |
| `ui_attr_damage` | 96×96 |
| `ui_attr_move_speed` | 96×96 |
| `ui_attr_attack_speed` | 96×96 |

---

## D. 技能（与 `Requirement_document.md` skillId 对齐）

| 文件名 | skillId |
|--------|---------|
| `ui_skill_shoot` | skill_shoot |
| `ui_skill_lightning` | skill_lightning |
| `ui_skill_thunder_fall` | skill_thunder_fall |
| `ui_skill_fire_rain` | skill_fire_rain |
| `ui_skill_water_slow` | skill_water_slow |
| `ui_skill_fire_ring` | skill_fire_ring |
| `ui_skill_locked` | 未解锁遮罩 |

槽位底：`ui_skill_slot_empty` 96×96，`ui_skill_slot_cd_mask` 96×96 半透明。

---

## E. Buff / 升级三选一

| 文件名 | 尺寸 | 说明 |
|--------|------|------|
| `ui_buff_atk` | 128×128 | 攻击类 |
| `ui_buff_def` | 128×128 | 防御类 |
| `ui_buff_spd` | 128×128 | 速度类 |
| `ui_buff_crit` | 128×128 | 暴击类 |
| `ui_buff_heal` | 128×128 | 治疗类 |
| `ui_buff_skill_up` | 128×128 | 技能升级 |
| `ui_frame_rarity_common` | 300×520 边框 | 灰 |
| `ui_frame_rarity_rare` | 300×520 | 青 |
| `ui_frame_rarity_epic` | 300×520 | 紫 |
| `ui_frame_rarity_legendary` | 300×520 | 金 |

> 具体 Buff 图标按 `UpgradeOptionSO` / `BuffSO` 配置扩展，命名 `ui_buff_{optionId}`。

---

## F. 装备（与 `EquipmentSlot` / `EquipmentQuality`）

| 文件名 | 说明 |
|--------|------|
| `ui_equip_slot_weapon` | 武器槽 |
| `ui_equip_slot_head` | 头部 |
| `ui_equip_slot_chest` | 胸甲 |
| `ui_equip_slot_boots` | 足部 |
| `ui_equip_slot_accessory` | 饰品 |
| `ui_equip_quality_common` | 框 |
| `ui_equip_quality_uncommon` | 框 |
| `ui_equip_quality_rare` | 框 |
| `ui_equip_quality_epic` | 框 |
| `ui_equip_quality_legendary` | 框 |
| `ui_equip_icon_battle_axe` | 示例武器 |
| `ui_equip_icon_leather_cap` | 示例 |
| `ui_equip_icon_warrior_chest` | 示例 |
| `ui_equip_icon_warrior_boots` | 示例 |

---

## G. 商城 / 抽奖 / 签到

| 文件名 | 尺寸 | 说明 |
|--------|------|------|
| `ui_shop_banner_free_diamond` | 920×160 | 12h 免费钻 |
| `ui_shop_tag_hot` | 80×32 | 热卖 |
| `ui_shop_tag_limit` | 80×32 | 限购 |
| `ui_gacha_platform` | 600×400 | 莲台底图 |
| `ui_gacha_btn_single` | 240×120 | 单抽 |
| `ui_gacha_btn_ten` | 240×120 | 十连 |
| `ui_signin_stamp_claimed` | 120×120 | 已签印章 |
| `ui_signin_day7_skill` | 160×160 | 第7日大图标 |

---

## H. 排行榜 / 成就

| 文件名 | 尺寸 |
|--------|------|
| `ui_rank_medal_1` | 96×96 |
| `ui_rank_medal_2` | 96×96 |
| `ui_rank_medal_3` | 96×96 |
| `ui_rank_tab_wave` | 120×48 |
| `ui_rank_tab_time` | 120×48 |
| `ui_rank_tab_power` | 120×48 |
| `ui_achieve_cat_battle` | 64×64 |
| `ui_achieve_cat_growth` | 64×64 |
| `ui_achieve_cat_collect` | 64×64 |
| `ui_achieve_seal_done` | 64×64 |

---

## I. 战斗 HUD _misc

| 文件名 | 尺寸 |
|--------|------|
| `ui_icon_wave` | 48×48 |
| `ui_icon_timer` | 48×48 |
| `ui_icon_kill` | 48×48 |
| `ui_icon_boss` | 64×64 |
| `ui_icon_elite_mode` | 64×64 |
| `ui_icon_mode_normal` | 120×120 |
| `ui_icon_mode_elite` | 120×120 |

---

## J. 可拉伸资源（9-Slice）

| 文件名 | 建议 Border (L,T,R,B) @1x |
|--------|---------------------------|
| `ui_panel_scroll_md` | 32,32,32,32 |
| `ui_panel_modal_lg` | 48,48,48,48 |
| `ui_btn_primary` | 24,24,24,24 |
| `ui_btn_secondary` | 24,24,24,24 |

Unity：Sprite Editor → Border 填入；Image Type **Sliced**。

---

## K. 伤害飘字（已有 Prefab 扩展）

| 文件名 | 说明 |
|--------|------|
| `ui_dmg_normal` | 可选图集数字 0-9 |
| `ui_dmg_crit` | 暴击样式 |
| `ui_dmg_heal` | 治疗绿 |

当前工程：`Assets/Prefabs/UI/DamageNumber.prefab` — 替换 TMP 样式或 Sprite 数字图集。

---

## Figma 批量导出步骤

1. 所有 Icon 放入 `99_Export_Slices`，命名与上表一致。
2. 选中图层 → Export **PNG 2x**。
3. 使用 Rename 插件或导出后脚本保持 `snake_case`。
4. 复制到 `Assets/UI/Sprites/Icons/{category}/`。
5. Unity：**Texture Type = Sprite (2D and UI)**，**Compression**：ASTC/ETC2（移动端）。

---

## 统计

| 类别 | 数量（约） |
|------|------------|
| 导航/系统 | 13 |
| 货币 | 7 |
| 属性 | 4 |
| 技能 | 8+ |
| Buff/框 | 10+ |
| 装备 | 14+ |
| 商城/抽奖/签到 | 10 |
| 排行/成就 | 10 |
| HUD | 8 |
| 九宫格 | 4 |
| **合计** | **~88**（随 Buff/商品配置递增） |
