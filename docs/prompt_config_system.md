# Config System 模块需求提示词

## 目标
建立 ScriptableObject 配置系统，让角色、敌人、技能、Buff、波次、Boss、掉落、音频、UI 文案和数值曲线全部可配置。

## 当前基础
`Stats` 和 `SkillSystem/Data` 已有少量数据类，但多数平衡参数仍写在 MonoBehaviour 或 Inspector 字段中，尚未形成统一配置入口。

## 输出要求
- 生成 `ConfigManager`：集中加载和缓存配置。
- 生成基础配置类型：`PlayerDataSO`、`EnemyDataSO`、`SkillDataSO`、`BuffDataSO`、`WaveDataSO`、`BossDataSO`、`DropTableSO`。
- 生成 `StatModifierConfig`：支持加法、乘法、最终倍率。
- 生成配置校验工具：检查空引用、负数、重复 ID、缺失图标和非法等级。
- 输出配置资产命名规范和存放路径。

## 实现流程
1. 先定义稳定 ID：`configId` 用于存档、事件和调试。
2. 将数值和展示字段放入 SO，将运行时状态留在 Controller。
3. 避免 SO 在运行时被直接修改，使用 RuntimeData 复制状态。
4. 为每类配置提供编辑器校验或运行时断言。

## 数据流
`ConfigManager` 加载 SO -> Manager 读取配置 -> Controller 创建 RuntimeData -> Event/UI 使用 RuntimeData 展示。

## 验收标准
- 新增敌人、技能、Buff 不需要改核心逻辑。
- 存档只保存 ID 和等级，不保存 SO 引用。
- 配置错误能在启动或编辑器阶段明确暴露。
