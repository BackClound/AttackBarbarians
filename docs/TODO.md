# 升级卡系统 — 待办事项

> 最后更新：2026-06-07  
> 范围：升级卡数据、奖励发放、各场景领取页面

---

## 任务 1：构建所有升级卡

### 已完成
- [x] 定义 `UpgradeCardCategory`（技能 / 属性 / 通用技能 / 通用属性）
- [x] 定义 `UpgradeCardSO`、`UpgradeCardRewardPoolSO` 数据模型
- [x] 实现 `UpgradeCardManager`（库存、权重抽取、通用卡解析）
- [x] 实现 `UpgradeCardConstants`（13 张卡 + 8 个奖池 configId）
- [x] 扩展 `SaveData`：`upgradeCardInventory`、`attributeBaseLevels`
- [x] 扩展 `ConfigDatabaseSO` / `ConfigManager` 索引升级卡与奖池
- [x] 提供 Editor 菜单：`Attack Barbarians → Upgrade Card → Create All Upgrade Cards & Reward Pools`

### 待完成
- [ ] 在 Unity 中执行上述 Editor 菜单，生成 `Assets/Resources/Config/UpgradeCard/` 资产
- [ ] 为 13 张升级卡配置图标（`icon` 字段，参考 `docs/version_02/ui_icon_export_manifest.md`）
- [ ] 实现升级卡**使用/消耗**逻辑：将卡片应用到技能等级或四维属性等级
- [ ] 实现 `attributeBaseLevels` 对局外战斗属性的实际加成（接入 `PlayerRuntimeStats` / 天赋管线）
- [ ] 升级卡背包 UI：查看库存、筛选技能卡/属性卡
- [ ] 为升级卡补充稀有度权重乘数（当前奖池仅用固定 weight）
- [ ] `ConfigValidator` 增加升级卡与奖池交叉校验

---

## 任务 2：各领取场景页面与随机奖励

### 已完成
- [x] `MetaRewardService`：在线时长、离线巡逻、抽奖、通关奖励
- [x] 商城宝箱：`ShopManager` 读取 `rewardConfigId` 奖池随机发卡
- [x] 七日签到：`DailyRewardEntrySO` 支持 `upgradeCardPoolConfigId`
- [x] 战斗结算：`RunRewardSettlementSO` 支持 `upgradeCardDrawCount`
- [x] `MainSceneRewardPanel` 接入真实计时与红点
- [x] `MetaRewardPagePanel`：在线 / 离线 / 抽奖 / 通关奖励页
- [x] `UpgradeCardRewardPopupPanel`：升级卡发放弹窗
- [x] `MainSceneBattlePageView` 绑定 Meta 奖励领取流程
- [x] `GameBootstrapper` 注册 `UpgradeCardManager`、`MetaRewardService`

### 待完成
- [ ] 在 `MainSceneBuilder` 中生成并绑定 `MetaRewardPagePanel`、`UpgradeCardRewardPopupPanel` 节点
- [ ] 在 `MainScene.unity` Inspector 中绑定 `metaRewardPage`、`upgradeCardPopup` 引用
- [x] 商城页 `ShopSceneView` / `ShopCrateWidget` 单抽/十连抽取后弹出 `ShopCrateRewardPopupPanel`（3 列九宫格 + 可滚动瀑布流）
- [x] 商城补给箱「奖励预览」按当前箱型展示对应奖池卡牌与爆率（普通箱 / 高级箱池子不同）
- [x] `UpgradeCardDisplayView` 卡片组件（英文标题 / 图标 / 中文名 / 描述 / 稀有度星）
- [ ] 为 13 张升级卡配置正式图标资源（替换占位色块）
- [ ] 战斗结算 `GameOverPanelUI` 展示本局获得的升级卡列表
- [ ] 七日签到 `DailyRewardPanelUI` 增加 7 日格子布局，并显示每日升级卡奖励预览
- [ ] 在线奖励计时改为独立 Coroutine 实时刷新（`MainSceneRewardPanel` 目前依赖 `RefreshAll`）
- [ ] 离线收益与免费钻石逻辑拆分（此前离线卡片误绑 `ShopManager.TryClaimFreeDiamond`）
- [ ] 抽奖系统扩展：多抽、保底计数、`SaveData.gachaPityCounter`
- [ ] 广告免费宝箱（`shop.ad_crate_*`）经 `AdRewardService` 发放时同步触发升级卡弹窗
- [ ] 重新执行 `Shop Config Bootstrap` 与 `Achievement & Daily Reward Bootstrap` 以写入宝箱/签到奖池引用

---

## 任务 3：配置与联调

### 待完成
- [ ] 确认 `GameSystems` 物体已挂载 `UpgradeCardManager`、`MetaRewardService`
- [ ] 执行 `Upgrade Card → Create All Upgrade Cards & Reward Pools`
- [ ] 执行 `Meta → Create Default Achievement & Daily Reward Assets`（若目录不存在）
- [ ] 执行 `Shop → Create Default Shop Assets` 后再次执行升级卡菜单（更新宝箱 `rewardConfigId`）
- [ ] 验证各来源发卡：
  - 商城普通/高级宝箱单抽与十连
  - 主场景在线 / 离线 / 通关奖励卡片
  - 幸运抽奖按钮
  - 七日签到（第 7 日应额外抽 2 张）
  - 战斗结束结算
- [ ] 旧存档迁移：确认 v2 → v3 后 `upgradeCardInventory` 字段正常初始化

---

## 奖励来源对照表

| 来源 | 服务 / Manager | 奖池 configId | UI 入口 |
|------|----------------|---------------|---------|
| 商城开宝箱 | `ShopManager` | `upgrade_card_pool.shop_crate_common/premium` | `ShopSceneView` |
| 离线奖励 | `MetaRewardService` | `upgrade_card_pool.offline_reward` | `MainSceneRewardPanel` |
| 在线奖励 | `MetaRewardService` | `upgrade_card_pool.online_reward` | `MainSceneRewardPanel` |
| 抽奖 | `MetaRewardService` | `upgrade_card_pool.lottery` | `MetaRewardPagePanel` / 侧边抽奖按钮 |
| 七日签到 | `DailyRewardManager` | `upgrade_card_pool.daily_reward` | 签到按钮 / `DailyRewardPanelUI` |
| 战斗结算 | `RunRewardSettlementService` | `upgrade_card_pool.run_settlement` | `GameOverPanelUI` |
| 通关奖励 | `MetaRewardService` | `upgrade_card_pool.stage_reward` | `MainSceneRewardPanel` |

---

## 升级卡清单（13 张）

### 技能升级卡（7 + 1 通用）
| configId | 对应技能 |
|----------|----------|
| `upgrade_card.skill.shoot` | Shoot |
| `upgrade_card.skill.fire_rain` | Fire Rain |
| `upgrade_card.skill.ice` | Ice |
| `upgrade_card.skill.lightning` | Lightning |
| `upgrade_card.skill.thunder` | Thunder |
| `upgrade_card.skill.water_wave` | Water Wave |
| `upgrade_card.skill.heal` | Heal |
| `upgrade_card.skill.generic` | 随机技能卡 |

### 属性升级卡（4 + 1 通用）
| configId | 对应属性（StatType） |
|----------|---------------------|
| `upgrade_card.attr.max_hp` | MaxHp |
| `upgrade_card.attr.damage` | Damage |
| `upgrade_card.attr.move_speed` | MoveSpeed |
| `upgrade_card.attr.attack_speed` | AttackSpeed |
| `upgrade_card.attr.generic` | 随机属性卡 |
