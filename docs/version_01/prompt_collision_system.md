# Collision System 模块需求提示词

## 目标
统一战斗物理查询（目标扫描、墙体探测、投射物命中），NonAlloc 优先，并与 Damage / Auto Attack 解耦。

## 输出要求
- `CollisionDataSO`、`CollisionManager`、`CollisionQuery`、`CollisionProfile`
- 注册到 `GameBootstrapper` / `ServiceLocator`
- 迁移 `PlayerTargetScanner`、`ProjectileController`、`Enemy` 墙体攻击射线

## 验收标准
- 所有高频 Overlap 使用 NonAlloc 或 Manager 共享缓冲
- Player / Enemy 状态机由 Controller 驱动
- 伤害结算仍只经 `DamagePipeline` / `DamageSystem`
