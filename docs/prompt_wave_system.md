# Wave System 模块需求提示词

## 目标
实现波次系统，控制敌人生成、波次计时、波次完成、难度成长、升级选择触发和自动进入下一波。

## 当前基础
`EnemyGenerateManager` 已实现按间隔随机生成敌人，但没有 WaveManager、WaveDataSO、敌人池、30 秒推进、击杀统计和波次事件闭环。

## 输出要求
- 生成 `WaveDataSO`：波次 ID、持续时间、敌人组合、生成间隔、最大数量、Boss 标记、奖励表。
- 生成 `WaveEnemyEntry`：敌人配置、数量、权重、生成时间段、属性倍率。
- 生成 `WaveManager`：StartWave、StopWave、SpawnNext、OnEnemyDied、CompleteWave。
- 生成 `SpawnAreaController`：根据相机和安全区计算生成范围。
- 波次完成后发布事件并触发随机升级选单。

## 实现流程
1. 保留当前生成位置算法，抽到 SpawnAreaController。
2. 用 WaveManager 替代 EnemyGenerateManager 的自动生成职责。
3. 使用对象池生成敌人。
4. 满足任一条件完成波次：敌人清空、达到 30 秒、Boss 被击败。
5. 完成时暂停生成，结算奖励，打开 Upgrade/Reward UI。

## 数据流
`WaveDataSO -> WaveManager -> EnemySpawnerManager -> PoolManager -> GameEvents.OnWaveCompleted`

## 验收标准
- 每波敌人数量、类型和间隔可配置。
- 达到 30 秒或清空敌人后能稳定进入下一波。
- 波次完成能触发升级、保存和 UI 刷新。
