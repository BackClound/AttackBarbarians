# Boss System 模块需求提示词

## 目标
实现 Boss System，支持阶段行为、技能释放、特殊机制、Boss 波次、专属 UI、奖励和存档统计。

## 当前基础
Enemy 系统已有普通敌人的状态机基础，但 Boss 尚未独立设计。`project_rules.md` 要求 Boss 支持，`work_flow.md` 将 Boss 放在内容系统阶段。

## 输出要求
- 生成 `BossDataSO`：ID、名称、Prefab、生命、攻击、阶段阈值、技能列表、奖励表。
- 生成 `BossController`：继承或组合 Enemy 基础能力。
- 生成 `BossPhase`：按血量、时间或事件切换阶段。
- 生成 `BossSkillDataSO`：冲锋、召唤、范围攻击、护盾、弹幕等技能配置。
- Boss 出现、阶段切换、死亡均发布事件。

## 实现流程
1. 复用 Enemy 的血量、受击、死亡和对象池基础。
2. Boss 独立处理阶段和技能冷却。
3. WaveManager 在指定波次触发 BossWave。
4. UI 订阅 Boss 事件显示 Boss 血条和阶段提示。
5. Boss 死亡触发特殊奖励和下一阶段内容解锁。

## 数据流
`WaveManager -> BossSpawner -> BossController -> BossPhase -> BossSkill -> DamageSystem/EventBus`

## 验收标准
- Boss 能在指定波次出现并阻止普通波次完成。
- Boss 阶段切换可配置且有 UI/音效反馈。
- Boss 死亡奖励和统计能被 Save/Reward 系统接收。
