## 阶段三：肉鸽系统

### 模板 5：实现肉鸽升级选单系统

目标
实现肉鸽（Roguelike）升级系统的核心逻辑和 UI 选单界面。

上下文
肉鸽升级在每波结束后触发（WaveManager 完成波次时调用 UpgradeManager.ShowUpgradeChoices()）

池中的升级从可用升级池中随机抽取 3 个不重复的选项

默认解锁技能为射击技能，当已解锁技能不足三个时，弹出的三个buff中包含未解锁技能的专属buff时，默认在当局解锁该技能，当已解锁技能为3个时，可选buff池中只包含通用buff和已解锁的三个随机技能对应的专属技能buff，如果在局外已解锁技能数大于等于三个时，可选buff池中只包含通用buff和局外已解锁技能对应的专属buff

buff升级效果在当局内生效，buff类型为通用buff时，该buff可以多次被选择，支持buff升级时效果叠加（例如暴击概率提升10%的buff，多个同样的buff效果可以进行叠加）,buff类型为技能专属buff时，在局内每次技能专属buff只能被选中一次，并且技能专属buff具有不同的等级时（例如增加弹道数量的buff，一级增加一个弹道，二级增加两个弹道），该类型buff会在三选一buff随机选择时，首先选中当前可选择的最低级buff，不可以在没有获得第一级buff的时候，随机到第二级buff，且专属技能buff效果不可叠加

当局游戏的buff增益效果，只在当局游戏生效（下次新建游戏时才重置）

在游戏开局时，首先加载局外已获得永久性buff增益，包含通用buff和专属技能buff

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

维护当前会话的已选 Buff 列表（List selectedBuffs）

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