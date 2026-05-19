# Content 与 Map System 模块需求提示词

## 目标
实现内容扩展与地图系统，支持不同地图、敌人生态、事件、特殊波次和后续 Addressables 内容加载。

## 当前基础
`work_flow.md` 第四阶段包含 More Skills、Maps、Events，`总结文档.md` 提到怪物生态、波次设计、Boss AI 和 Addressables 热更新架构。

## 输出要求
- 生成 `MapDataSO`：地图 ID、背景、出生区域、墙体位置、推荐难度、BGM。
- 生成 `MapManager`：加载地图、设置相机边界、初始化生成区域。
- 生成 `GameplayEventDataSO`：随机事件 ID、触发条件、持续时间、效果。
- 生成 `ContentRegistry`：登记敌人、技能、Boss、地图和事件配置。
- 预留 Addressables 加载接口，但不强制引入复杂热更新。

## 实现流程
1. 先将当前固定战斗场景抽象为默认地图配置。
2. WaveManager 从 MapDataSO 读取生成区域和地图修正。
3. 随机事件通过 EventBus 影响 Wave、Buff、Enemy 或 Reward。
4. 内容配置集中校验，避免缺失 Prefab 或重复 ID。

## 数据流
`MapDataSO -> MapManager -> SpawnArea/WaveManager/UI/Audio`

## 验收标准
- 默认地图可配置生成范围和背景。
- 新增地图不需要修改核心战斗逻辑。
- 随机事件能在指定条件下触发并结束。
