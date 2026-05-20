第一阶段：基础框架
Core Framework
Event System
Singleton Framework
Object Pool
Save System
Config System
Game State Machine

场景挂载（Core Framework 已完成部分）
- 在首个可玩场景创建空物体 `GameSystems`，挂 `GameBootstrapper`；同物体或子物体挂 `ConfigManager`、`SaveManager`、`GameManager`；子物体 `PoolRoot` 挂 `PoolManager`。
- 勿将 Manager 挂在 Player / Enemy / UI 上；`EventBus`、`ServiceLocator` 不挂载。
- 配置资产：`Assets/Resources/Config/GameConfig.asset`。
- 完整说明见 `docs/core_framework_scene_setup.md`。

第二阶段：战斗核心
Player
Enemy
Damage System
Projectile System
Auto Attack
Collision
Wave Spawn

第三阶段：成长系统多层次性· 
Skill System
Buff System
Upgrade System
Talent System
Equipment System

第四阶段：内容系统
Boss
Elite
Special Enemy
More Skills
Maps
Events

第五阶段：外围系统
UI
Audio
Save
Achievement
Daily Reward
Shop

第六阶段：优化
性能优化
GC优化
内存优化
移动端优化
Addressables

重构Player和Enemy的StateMachine有限状态机以及shoot的旧逻辑代码，移除旧的更新血量，攻击以及受攻击的逻辑，使用现有的core framework框架进行实现