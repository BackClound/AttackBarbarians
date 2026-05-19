# Economy 与 Shop System 模块需求提示词

## 目标
实现经济与商店系统，管理金币、钻石、局外升级消耗、商店购买、广告/签到奖励预留和资源变更 UI。

## 当前基础
规则和文档中提到金币、钻石、商店 UI、永久成长，但当前代码尚未有资源管理或商店逻辑。

## 输出要求
- 生成 `CurrencyType`：Gold、Diamond、Energy 等。
- 生成 `ResourceManager`：增加、消耗、查询资源。
- 生成 `ShopItemSO`：商品 ID、名称、价格、限购、奖励内容。
- 生成 `ShopManager`：购买校验、扣费、发奖、刷新。
- 资源变化通过事件通知 UI，并写入 SaveData。

## 实现流程
1. 资源变化只能通过 ResourceManager。
2. 商店购买先校验资源和限购，再发放奖励。
3. 所有资源变更记录来源，便于调试和埋点。
4. 商店 UI 不直接改存档。

## 数据流
`Enemy/Reward/Shop -> ResourceManager -> SaveManager -> UI`

## 验收标准
- 金币和钻石可增加、消耗、保存和刷新 UI。
- 商店购买失败有明确原因。
- 商品可通过配置扩展。
