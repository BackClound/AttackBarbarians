
### 2.4 `roguelike-system.mdc`

```yaml
---
description: "肉鸽（Roguelike）随机升级系统开发规范"
globs: ["Assets/Scripts/Roguelike/**/*.cs", "Assets/Scripts/Data/RuneSO.cs", "Assets/Scripts/Core/UpgradeManager.cs"]
alwaysApply: false
---

# 肉鸽系统规范

## 核心机制
- 每波（或每 N 波）结束后触发肉鸽升级选单，提供 3 个随机 Buff/Rune 供玩家选择。
- 肉鸽升级定义在 ScriptableObject 中（`RuneSO`），包含：
  - `DisplayName`：升级名称
  - `Description`：升级描述
  - `Icon`：显示图标
  - `EffectType`：效果类型（`StatBoost`、`TowerModifier`、`GlobalEffect`、`NewTower` 等）
  - `ApplyEffect()`：应用逻辑接口
  - `TowerTypeAffected`：影响的目标塔类型（如仅弓箭塔受益）
- 肉鸽效果应支持堆叠（多次选择同一升级时效果叠加或升级）。
- 肉鸽效果的数据存储使用 `PlayerRoguelikeData` 持久化保存。

## 效果类型参考
| 效果类型 | 示例 | 实现方式 |
|---------|------|---------|
| StatBoost | 塔攻击力 +15% | 修改 Towers 的 data 引用或添加 Buff 组件 |
| TowerModifier | 塔攻击速度翻倍 | 动态修改 Tower 的攻击速度 |
| GlobalEffect | 所有塔射程 +20% | 修改全局塔属性管理器的默认属性 |
| NewTower | 解锁新塔类型 | 在 BuildMenu 启用对应的塔选项 |
| EconomyBoost | 杀敌金币 +30% | 修改击杀奖励计算逻辑 |

## ScriptableObject 数据模板
```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "NewRune", menuName = "TowerDefense/Rune")]
public class RuneSO : ScriptableObject
{
    public string runeName;
    [TextArea] public string description;
    public Sprite icon;
    public RuneEffectType effectType;
    public float value;  // 效果数值（如 +0.15 表示 +15%）
    public TowerType affectedTowerType;
    
    public virtual void ApplyEffect()
    {
        // 根据 effectType 分发给不同系统处理
    }
}