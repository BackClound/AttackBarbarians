## 阶段三：肉鸽系统

### 模板 5：实现肉鸽升级选单系统
目标
实现肉鸽（Roguelike）升级系统的核心逻辑和 UI 选单界面。

上下文
肉鸽升级在每波结束后触发（WaveManager 完成波次时调用 UpgradeManager.ShowUpgradeChoices()）

升级效果全局影响后续波次，支持多选同一种升级时效果堆叠

池中的升级从可用升级池中随机抽取 3 个不重复的选项

选择某个升级后，UpgradeManager 调用对应的升级效果并添加到已选列表中

当前会话的游戏升级数据应持久化（下次新建游戏时才重置）

输出要求
生成 BuffSO.cs：

字段：buffName(string)、description(string)、icon(Sprite)、effectType(BuffEffectType 枚举)、value(float)

方法：ApplyEffect()（多态实现）

生成具体升级效果类（继承自 BuffSO）：

AttackDamageBuff（攻击力 +X%）

AttackSpeedBuff（攻击速度 +X%）

PierceAbilityBuff（塔射程 +X%）

CritDamageBUff（爆击 +X%）

NewSKillBuff（解锁新技能类型）

SkillLevelUpgradeBuff（技能等级类型）

生成 UpgradeManager.cs：

维护当前会话的已选 Buff 列表（List<BuffSO> selectedBuffs）

方法：GetRandomChoices(int count) 从配置的 master upgrade pool 抽取

方法：SelectBuff(BuffSO buff) 调用 buff.ApplyEffect() 并将其加入已选列表

支持升级效果的堆叠统计（例如攻击力 +15% 再 +15% = +30%）

生成 UpgradeChoicePanel UI 界面：

包含 3 个选择卡片，每个卡片包含图标、名称、描述和选择按钮

选择后关闭 Panel 并恢复游戏

动画效果：Panel 弹出时淡入淡出 + 轻微缩放

效果堆叠要求
UpgradeManager 应维护效果字典（Dictionary<BuffEffectType, float>），应用于全局系统

新增Buff时自动应用全局效果

升级数据应可持久化保存到 PlayerPrefs 或本地 JSON 文件中，用于未来的永久成长系统（肉鸽 Lite）