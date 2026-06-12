# SDD-10 经济与商城系统

| 项 | 内容 |
|----|------|
| 版本 | v1.0 |
| 关联 PRD | §11 经济 · §18 商业化 |
| 架构层 | L5 Meta |
| 实现度 | ✅ 80% |

---

## 1. 系统概述

四种货币管理、体力恢复、商城购买、免费钻、广告补给、广告券兑换与宝箱发卡。

---

## 2. 核心类

| 类 | 路径 |
|----|------|
| `ResourceManager` | `Economy/ResourceManager.cs` |
| `ShopManager` | `Shop/ShopManager.cs` |
| `AdRewardService` | `Ads/AdRewardService.cs` |
| `DailyRewardManager` | `DailyReward/DailyRewardManager.cs` |

---

## 3. 货币

| CurrencyType | 存档字段 | UI 名 |
|--------------|----------|-------|
| Gold | gold | 废料金 |
| Diamond | diamonds | 量子钻 |
| AdTicket | adTickets | 广告券 |
| Stamina | energy/maxEnergy | 体力 |

**体力**：上限 30，开战耗 5，每点恢复 120s。

---

## 4. 商城

### ShopManager API

- `TryPurchase(itemConfigId)`  
- `TryClaimFreeDiamond()` — 12h 冷却  
- `TryClaimAdFreeSupply()` — 广告补给/宝箱  
- `TryExchangeAdTickets()` — 券兑换  

### 宝箱

`configId` 含 `crate` → `GrantUpgradeCardReward(poolId)`

### 已生成商品（11 个）

金币包、钻石包、普通/高级宝箱单抽十连、金币补给等。

### 未生成资产

`shop.free_diamond`, `shop.ad_crate_*`, `shop.exchange_*` 等（逻辑在 Manager，资产待 Bootstrap）

---

## 5. 广告

| 提供者 | 说明 |
|--------|------|
| MockAdSdk | 编辑器 |
| UnityAdsAdSdk | 真机 |

集成：商店补给、体力回满、广告券奖励。

**缺口**：广告宝箱发卡后未同步升级卡弹窗。

---

## 6. 商业化（二期）

| 项 | 状态 |
|----|------|
| IAP | ❌ |
| Banner | ❌ |
| 月卡/首充 | ❌ 入口占位 |

---

## 7. 实现状态分析

| 项 | 状态 |
|----|------|
| 资源增减/存档/事件 | ✅ |
| 体力恢复 UTC | ✅ |
| 商城购买/限购 | ✅ |
| 宝箱单抽十连 UI | ✅ |
| 奖池预览 | ✅ |
| 部分商品资产 | ⚠️ |
| 抽奖保底 | ❌ |
| IAP | 🔜 |

---

## 8. 测试要点

- 余额不足购买失败  
- 免费钻 12h 边界  
- 十连扣费 10 次、发卡 10 张  
