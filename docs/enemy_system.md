## 阶段二：enemy核心机制

### 模板 3：实现enemy的生成与可成长性系统
目标
实现无线防守模式+肉鸽类游戏机制下的enemy的随机生成机制与随波数增加enemy四维属性提升的机制

上下文
基于阶段一已创建的 Project 结构和管理器框架

为不同的波数设置不同的enemy池， 在enemy池中随机选择enemy生成，enemy生成之后沿直线向player移动，到达player侧之后，触发攻击。
enemy的选择，生成数量，生成间隔，生成位置的添加随机属性
波次自动推进：当前波次所有敌人生成并消灭后，进入下一波或者当前波次达到30s后自动进入下一波
 
enemy拥有基础的移动、攻击、爆击、生命值，经验值、受击特效等数据， 各种数据定义在 Entity_Stats.cs 和 EntityStatsDataSO（ScriptableObject）中

enemy的攻击目标类型：Player

Enemy减益buff： 收到Player的技能攻击之后，会获得对应的减益buff，闪电造成麻痹效果，落雷造成区域禁锢效果，火系造成持续伤害效果，水系/冰系造成减速效果

Enemy 使用对象池管理

输出要求
生成enemy孵化管理类 EnemySpawnerManager.cs：
通过enemy池及WaveManager控制Enemy的创建与回收
生成 EnemyDataSO.cs：
包含字段：enemyName（string）、maxHealth (float)、moveSpeed (float)、rewardGold (int)、rewardExp (int)
生成 Enemy.cs：
核心属性：currentHealth
核心方法：TakeDamage(float damage)、Move()、DoDamage()
受击特效（HitFlash 改变颜色或播放受击动画）

生成 WaveDataSO.cs：
包含 List<WaveEnemyEntry>（敌人类型+数量组合）
生成 WaveManager.cs：
核心方法：StartWave()、SpawnNextEnemy()、OnEnemyDied()、OnWaveCompleted()
管理当前波次索引和波次完成事件发布
支持在波次之间触发肉鸽升级选单

对象池集成
Enemy 应实现 IPoolable，在 OnSpawn 时重置生命值，在当前enemy池失效时，回收清除所有enemy

WaveManager 从池中 GetEnemy 而非常规实例化