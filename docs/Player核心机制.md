## 阶段二：Player核心机制

### 模板 3：实现Player的技能升级功能
目标
实现无线防守模式+肉鸽类游戏机制下的Player攻击系统

上下文
基于阶段一已创建的 Project 结构和管理器框架

Player拥有基础的射击技能以及闪电技能，落雷技能，火系技能，水系技能，冰系技能以及恢复技能， 各种技能的数据定义在 xxxSkillDataSO（ScriptableObject）中

Player的攻击目标类型：优先攻击射程内距离终点最近的敌人或血量最低的敌人

Player是场景中预定义的固定位置，player位于屏幕底部，敌人从屏幕顶部生成

Player获得buff增益的机制，获得一项技能升级buff，后面三个是基础属性升级buff，然后循环

Player的投射物（子弹/闪电/落泪等）使用对象池管理

输出要求
生成基础四维属性升级buff xxxBaseBuffSO.cs：
包含字段：属性名称、图标、攻击力增幅(float)、攻击速度增幅(float)、攻击范围增幅(float)、爆击伤害增幅(float)、爆击概率增幅(float)
生成技能升级buff xxxSkillBuffSO.cs:
包含字段：技能名称，图标，弹道增幅（int）、射速增幅、穿透增幅、弹射增幅、技能伤害增幅，技能范围增幅，技能持续时间增幅，技能冷却增幅

包含针对不同等级的属性访问方法（GetAttackDamageForLevel(int level) 等）

生成 Player.cs：

核心属性：data (XXXSkillDataSO)、currentLevel (int) 、XXXBuffSO.cs

核心方法：FindTarget()（根据 targetType 锁定敌人）、Attack()（进行攻击）、Upgrade()（检查资源并执行升级）

使用 GameEvents 发布升级事件

生成 SkillObject_Bullet.cs, SkillObject_Thunder.cs, SkillObject_Fire, SkillObject_Lighting.cs：

实现 IAttackable 接口（OnSpawn 重置位置和状态、OnDespawn 回收、DoDamage 进行攻击）

核心逻辑：向 target 移动，命中 Enemy 时扣血并返回池中

性能要求
塔的 Update 中检测目标使用缓存优化，避免每帧重复 Find

攻击冷却使用独立的计时器变量，每次攻击后重置

FindTarget 使用 Physics2D.OverlapCircleAll 进行高效的射程检测
SKillObject 移动逻辑使用 MoveTowards 配合速度和时间，确保帧率自适应