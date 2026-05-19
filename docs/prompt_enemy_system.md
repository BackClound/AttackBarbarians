# Enemy System 模块需求提示词

## 目标
完善敌人系统，支持普通怪、精英怪、特殊怪、Boss 前置能力、属性成长、受击反馈、死亡奖励和对象池回收。

## 当前基础
已有 `Enemy`、`BatEnemy`、敌人状态机、`Enemy_Health`、`EnemyCombatManager` 和 `EnemyGenerateManager`。当前敌人直接实例化，波次和数据配置尚不完整，受击上下文只传 float damage。

## 输出要求
- 生成 `EnemyDataSO`：ID、名称、Prefab、生命、攻击、移速、护甲、奖励、能力标签。
- 生成 `EnemyController` 或改造现有 `Enemy`：接收 RuntimeData，管理状态机。
- 生成 `EnemySpawnerManager`：由 WaveManager 驱动，从对象池获取敌人。
- 支持普通、冲锋、护盾、分裂、召唤、远程等能力接口。
- 死亡时发布 `OnEnemyDied`，携带奖励和来源。

## 实现流程
1. 将敌人生成从 `EnemyGenerateManager` 迁移到 Wave/Pool 驱动。
2. 敌人 OnSpawn 重置血量、Buff、状态、Collider、位置。
3. 受击统一走 `DamageSystem.ApplyDamage(DamageInfo)`。
4. 死亡不直接 Destroy，等待死亡动画后回收到池。
5. 敌人奖励通过事件交给 Upgrade/Resource 系统处理。

## 数据流
`WaveDataSO -> EnemySpawnerManager -> PoolManager -> EnemyController -> DamageSystem -> GameEvents.OnEnemyDied`

## 验收标准
- 不同波次可配置不同敌人池。
- 敌人随波次获得属性缩放。
- 敌人死亡、奖励、回收、UI 飘字均通过统一事件流触发。
