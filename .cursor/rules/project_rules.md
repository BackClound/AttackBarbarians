1. 项目名称
Attack Barbarians

游戏类型
2D 无限防守模式+ Roguelike

核心玩法
玩家防守城墙。
怪物从地图外不断生成并移动到城墙。

玩家通过：
自动攻击
技能释放
Buff强化基础属性及技能效果
局内随机升级
来抵御无限怪潮。

核心循环
战斗 → 杀怪 → 获得经验 → 升级 → 三选一Buff → 强化Player → 更高波次

2. 核心系统
必须实现以下系统：
Player System
Enemy System
Skill System
Buff System
Weapon System
Wave System
Boss System
Upgrade System
Random Reward System
Damage System
UI System
Audio System
Save System
Event System
Object Pool System

3. 技术架构
必须采用：
Manager + Controller + Config
ScriptableObject配置驱动
EventBus事件系统
对象池
数据驱动技能
配置驱动Buff

4. 技能系统要求
技能必须支持：
自动释放
多段伤害
DOT
范围伤害
连锁
穿透
弹射
暴击
属性伤害
技能进化
技能等级成长

5. Buff系统要求
Buff必须支持：
叠加
永久Buff
临时Buff
被动Buff
光环Buff

6. 敌人系统要求
敌人必须支持：
普通怪
Boss
护盾
冲锋
分裂
召唤


7. UI要求
必须实现：
HUD
血条
波次UI
Buff UI
技能UI
伤害飘字
结算界面
升级三选一
暂停界面
商店UI

8. 数据规范
所有数据必须配置化：
技能
Buff
怪物
Boss
波次
掉落
数值

9. 文件结构
Assets/ ├── Art/ ├── Audio/ ├── Prefabs/ ├── Resources/ ├── ScriptableObjects/ ├── Scenes/ ├── Scripts/ │ ├── Core/ │ ├── Managers/ │ ├── Gameplay/ │ ├── UI/ │ ├── Data/ │ ├── Systems/ │ ├── Pool/ │ ├── Utilities/ │ └── Editor/ ├── Addressables/ └── ThirdParty/

10. 代码要求
必须：
高内聚低耦合
可扩展
可维护
可测试
移动端优化
避免GC
使用对象池
AI开发要求

11. Cursor每次生成代码前：
必须先分析：
模块依赖
文件结构
数据流
事件流
扩展性
性能影响