# Performance Optimization 模块需求提示词

## 目标
针对 2D 无限防守 + Roguelike 的大量敌人、子弹、特效和 UI 更新场景进行性能优化，目标移动端稳定 60 FPS。

## 当前基础
`性能优化与调试.md` 已提出对象池、Update 优化、Physics2D 优化和 UI 优化。当前代码仍存在直接 Instantiate/Destroy、高频 Debug.Log、每帧检测和未统一池化等风险。

## 输出要求
- 输出 Profiler 检查清单：CPU、GC、Physics2D、Rendering、UI、Memory。
- 输出性能预算：敌人数量、子弹数量、伤害数字数量、同时音效数量。
- 优化目标扫描：降低频率，使用 `OverlapCircleNonAlloc`。
- 优化对象生命周期：所有高频对象接入 Pool。
- 优化日志：增加 Debug 开关，禁止战斗高频路径默认日志。

## 实现流程
1. 先建立基准场景和性能指标。
2. 用 Profiler 找到最高成本路径。
3. 优先优化对象创建、物理检测、UI 重建和日志。
4. 每次优化后记录对比数据。
5. 对移动端开启质量分级和特效数量限制。

## 重点检查
- `EnemyGenerateManager` 的 Instantiate 替换为 Pool。
- `SkillShoot` 和 `PlayerCombat` 的目标扫描合并并降频。
- `DamageNumberController` 的回收链路完整。
- 子弹和敌人死亡不直接 Destroy。
- Update 中不做资源加载、组件查找和临时集合分配。

## 验收标准
- 大量敌人和子弹同时存在时 GC Alloc 接近 0。
- 伤害数字、子弹、敌人复用稳定。
- Profiler 对比能证明优化前后差异。

## 验收标准补充
- 本模块完成后必须形成最小可运行闭环，可以在当前 Unity 场景中通过手动挂载或现有入口验证核心流程。
- 相关配置、运行时数据、事件发布和事件订阅必须有明确入口，异常或缺失配置时要给出可定位的问题表现。
- 高频逻辑不得引入明显 GC 分配；涉及生成、销毁、特效、投射物、敌人或 UI 飘字时必须优先接入对象池。
- 完成实现后必须说明受影响脚本、Prefab/Scene 挂载要求、测试步骤和未迁移的旧逻辑边界。

## 模块依赖边界
- 本模块只能依赖已完成或同阶段明确约定的公共接口、配置数据和事件 Key，不直接依赖后续阶段的具体实现类。
- 跨模块通信优先通过 `EventBus`、`IGameSystem`、`ServiceLocator`、ScriptableObject 配置或明确的 Controller API 完成。
- 禁止从低层模块反向引用高层模块；例如 Damage、Pool、Config 不应依赖 UI、Upgrade、Shop 等外围系统。
- 禁止在核心逻辑中散落 `FindObjectOfType`、硬编码场景路径或直接访问无关单例；必须通过启动流程、序列化引用或服务注册注入依赖。
- 数据结构与事件 Payload 必须保持小而稳定，避免为了单个功能暴露整个 Manager 或运行时对象内部状态。

## 旧逻辑兼容与迁移约束
- 保持当前可运行逻辑，不一次性删除或重写现有 `Player`、`Enemy`、`SkillShoot`、`EnemyGenerateManager` 等已在场景中使用的脚本。
- 新系统先以适配器、旁路入口或可切换开关接入；确认新流程可运行后，再逐步迁移旧职责。
- 每次迁移只替换一个清晰职责，例如目标选择、伤害结算、对象生成、UI 刷新或配置读取，避免跨系统大改。
- 迁移期间必须保持旧 Prefab、Animator、Collider、Layer、Tag 和 Inspector 序列化字段不失效。
- 删除旧代码前必须确认没有场景引用、Prefab 引用和运行时调用；无法确认时保留兼容层并标注后续清理任务。

