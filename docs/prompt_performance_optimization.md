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
