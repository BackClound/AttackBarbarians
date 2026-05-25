# 程序集分阶段迁移（asmdef）

> 对应 `architecture_design.md` §6.1 · 阶段 6 工作流项

## 目标结构

| 程序集 | 脚本目录（asmref） | 依赖 |
|--------|-------------------|------|
| `AB.Core` | Singleton、L0、Contracts、Events、Pool、Utils/DeviceInfo | UnityEngine |
| `AB.Config` | Config、Data、Stats | AB.Core |
| `AB.Combat` | Player、Enemy、Damage、Projectile、Collision、AutoAttack、Common、Wall、Interface | AB.Core、AB.Config |
| `AB.Skills` | SkillSystem | + AB.Combat |
| `AB.Gameplay` | Wave、Boss、SpecialEnemy、Map、Upgrade、Elite、Content | + AB.Skills |
| `AB.Meta` | Save、Talent、Equipment、Shop、Economy、Achievement、DailyReward | + AB.Gameplay |
| `AB.Presentation` | UI、Audio、Performance、ObjectVFX | + AB.Meta |
| `AB.App` | Core/App、Managers、Utils | 全部运行时程序集 |
| `AB.Editor` | Editor/Config | 全部运行时 + Editor |

程序集定义文件：`Assets/Scripts/_Assemblies/{Name}/{Name}.asmdef`（不直接放业务脚本）。

## 分阶段推进

| 阶段 | 枚举值 | 内容 |
|------|--------|------|
| 0 | `None` | 默认 `Assembly-CSharp` |
| 1 | `CoreSingleton` | `Core/Singleton` → AB.Core |
| 2 | `CoreFoundation` | 搬移 Contracts/L0；Events、Pool → AB.Core |
| 3 | `Config` | Config、Data、Stats；`SaveConstants` 迁入 Config |
| 4 | `Combat` | 战斗层目录 |
| 5 | `Skills` | SkillSystem |
| 6 | `Gameplay` | 内容层 |
| 7 | `Meta` | 养成 / 存档 |
| 8 | `Presentation` | UI、音频、性能表现 |
| 9 | `App` | GameBootstrapper、Managers |
| 10 | `Editor` | Editor/Config → AB.Editor |

## Unity 菜单

```
Attack Barbarians/Assembly/
├── Status Report
├── Apply Phase 1 (Core Singleton)
├── Apply Phase 2 (Core Foundation)
├── Apply Phase 3 (Config)
├── Apply Through Phase...
├── Apply Complete Migration (Phase 10 Editor)
└── Reset All Asmrefs
```

**推荐流程：** 每应用一阶段 → 等待脚本编译 → Console 无 error → 运行 BattleScene 冒烟 → 再进下一阶段。

## 代码入口

- `AssemblyMigrationPhase` / `AssemblyMigrationCatalog`：`Assets/Scripts/Core/`
- Editor 工具：`Assets/Editor/AssemblyMigration/`（保持默认 Editor 程序集，避免迁移工具自身循环依赖）

## 已做的解耦（支持 Phase 2 Pool → AB.Core）

- `PoolManager` 不再引用 `ConfigManager`；由 `GameBootstrapper` 在 `Initialize` 前调用 `ConfigureRuntimePolicy`。
- 共享枚举计划在 Phase 2 搬至 `Assets/Scripts/Contracts/`（`ElementType`、`ProjectileMotionType`）。

## 回滚

`Reset All Asmrefs` 仅删除 `.asmref`，**不**自动还原 `AssetDatabase.MoveAsset` 的搬移；若需完全回滚请用版本控制恢复。

## 验收

- [ ] Phase 1 编译通过，Singleton 行为不变
- [ ] Phase 3 后 Config / GameConfig 可加载
- [ ] Phase 9 后场景内 `GameBootstrapper` 正常（脚本位于 `Core/App`）
- [ ] Phase 10 后 Editor 菜单与运行时程序集均编译通过
- [ ] 无 asmdef 循环依赖警告
