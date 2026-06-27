using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家运行时属性：基础配置 + 局内 Buff + 永久成长修正，并同步到 <see cref="Entity_Stats"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。由 <see cref="PlayerController"/> 创建并驱动。</para>
/// </remarks>
public sealed class PlayerRuntimeStats
{
    private readonly PlayerRuntimeData runtimeData = new PlayerRuntimeData();
    private readonly StatRuntimeSnapshot workingSnapshot = new StatRuntimeSnapshot();
    private readonly List<BuffRuntimeData> permanentActiveBuffs = new List<BuffRuntimeData>(8);
    private readonly List<BuffRuntimeData> runScopedActiveBuffs = new List<BuffRuntimeData>(8);
    private readonly List<StatModifierConfig> extraModifiers = new List<StatModifierConfig>(16);
    private readonly List<StatModifierConfig> talentModifiers = new List<StatModifierConfig>(16);
    private readonly List<StatModifierConfig> equipmentModifiers = new List<StatModifierConfig>(16);

    private PlayerDataSO sourceData;
    private float configuredAttackRadius = 25f;

    /// <summary>局内运行时数据（等级、经验等）。</summary>
    public PlayerRuntimeData Data => runtimeData;
    /// <summary>当前属性快照。</summary>
    public StatRuntimeSnapshot Snapshot => workingSnapshot;
    /// <summary>是否已完成 Initialize。</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>从配置与存档初始化运行时属性。</summary>
    /// <param name="data">玩家基础配置。</param>
    /// <param name="save">存档数据，可为空。</param>
    public void Initialize(PlayerDataSO data, SaveData save = null)
    {
        sourceData = data;
        if (data == null)
        {
            IsInitialized = false;
            return;
        }

        configuredAttackRadius = data.AttackRadius;
        runtimeData.Initialize(data);
        workingSnapshot.CopyFromBlock(data.BaseStats);
        workingSnapshot.Set(StatType.AttackRadius, configuredAttackRadius);

        ApplyPermanentGrowthFromSave(save);
        RebuildSnapshot();
        IsInitialized = true;
    }

    /// <summary>获取攻击射程。</summary>
    /// <returns>当前攻击半径。</returns>
    public float GetAttackRadius() => workingSnapshot.Get(StatType.AttackRadius);

    /// <summary>读取指定属性值。</summary>
    /// <param name="statType">属性类型。</param>
    /// <returns>快照中的属性值。</returns>
    public float Get(StatType statType) => workingSnapshot.Get(statType);

    /// <summary>天赋系统入口：替换局外天赋修正列表（由 <see cref="TalentManager"/> 驱动）。</summary>
    public void SetTalentModifiers(IReadOnlyList<StatModifierConfig> modifiers)
    {
        talentModifiers.Clear();
        if (modifiers == null)
        {
            RebuildSnapshot();
            return;
        }

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifierConfig modifier = modifiers[i];
            if (modifier != null)
            {
                talentModifiers.Add(modifier);
            }
        }

        RebuildSnapshot();
    }

    /// <summary>装备系统入口：替换局外装备修正列表（由 <see cref="EquipmentManager"/> 驱动）。</summary>
    public void SetEquipmentModifiers(IReadOnlyList<StatModifierConfig> modifiers)
    {
        equipmentModifiers.Clear();
        if (modifiers == null)
        {
            RebuildSnapshot();
            return;
        }

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifierConfig modifier = modifiers[i];
            if (modifier != null)
            {
                equipmentModifiers.Add(modifier);
            }
        }

        RebuildSnapshot();
    }

    /// <summary>Buff / Upgrade 系统入口：叠加单条属性修正。</summary>
    public void ApplyModifier(StatModifierConfig modifier)
    {
        if (modifier == null)
        {
            return;
        }

        extraModifiers.Add(modifier);
        RebuildSnapshot();
    }

    /// <summary>
    /// 清除局内临时属性修正与 StatBuff（<see cref="ApplyBuff"/> 写入项），
    /// 保留天赋、装备与 <see cref="SaveData.permanentUpgrades"/> 永久成长 Buff。
    /// </summary>
    public void ClearRunScopedModifiers()
    {
        bool changed = extraModifiers.Count > 0 || runScopedActiveBuffs.Count > 0;
        extraModifiers.Clear();
        runScopedActiveBuffs.Clear();
        if (changed)
        {
            RebuildSnapshot();
        }
    }

    /// <summary>局内 Buff 系统入口：应用 StatBuff 配置并重建属性（局末由 <see cref="ClearRunScopedModifiers"/> 清除）。</summary>
    public void ApplyBuff(BuffDataSO buff, int stacks = 1)
    {
        if (buff == null)
        {
            return;
        }

        runScopedActiveBuffs.Add(ConfigRuntimeFactory.CreateBuff(buff, stacks));
        RebuildSnapshot();
    }

    /// <summary>移除指定 Buff 并重建属性（优先匹配局内 Buff，其次永久 Buff）。</summary>
    /// <param name="buffConfigId">Buff 配置 Id。</param>
    public void RemoveBuff(string buffConfigId)
    {
        if (string.IsNullOrEmpty(buffConfigId))
        {
            return;
        }

        int removed = runScopedActiveBuffs.RemoveAll(b => b != null && b.ConfigId == buffConfigId);
        removed += permanentActiveBuffs.RemoveAll(b => b != null && b.ConfigId == buffConfigId);
        if (removed > 0)
        {
            RebuildSnapshot();
        }
    }

    /// <summary>Tick 所有 Buff 持续时间并在过期时重建属性。</summary>
    /// <param name="deltaTime">帧间隔（秒）。</param>
    public void TickBuffs(float deltaTime)
    {
        if (runScopedActiveBuffs.Count == 0 && permanentActiveBuffs.Count == 0)
        {
            return;
        }

        bool changed = TickBuffList(runScopedActiveBuffs, deltaTime);
        changed |= TickBuffList(permanentActiveBuffs, deltaTime);
        if (changed)
        {
            RebuildSnapshot();
        }
    }

    /// <summary>将快照同步到 Entity_Stats 并发布属性变更事件。</summary>
    /// <param name="entityStats">目标实体属性组件。</param>
    /// <param name="eventSender">事件发送方。</param>
    public void ApplyToEntityStats(Entity_Stats entityStats, object eventSender)
    {
        if (entityStats == null)
        {
            return;
        }

        ConfigStatBridge.ApplyToEntityStats(workingSnapshot, entityStats);
        GameEvents.RaisePlayerStatsChanged(eventSender, new PlayerStatsChangedEventArgs(workingSnapshot));
    }

    /// <summary>重建工作快照（基础值 + 修正 + Buff）。</summary>
    private void RebuildSnapshot()
    {
        if (sourceData == null)
        {
            return;
        }

        workingSnapshot.CopyFromBlock(sourceData.BaseStats);
        workingSnapshot.Set(StatType.AttackRadius, configuredAttackRadius);

        var combinedModifiers = CollectAllModifiers();
        if (combinedModifiers.Count > 0)
        {
            workingSnapshot.ApplyModifiers(combinedModifiers);
        }

        runtimeData.Stats.CopyFrom(workingSnapshot);
    }

    /// <summary>汇总局内修正、天赋、装备与 Buff 修正列表。</summary>
    /// <returns>合并后的修正列表。</returns>
    private List<StatModifierConfig> CollectAllModifiers()
    {
        var combined = new List<StatModifierConfig>(
            extraModifiers.Count + talentModifiers.Count + equipmentModifiers.Count + 8);
        combined.AddRange(extraModifiers);
        combined.AddRange(talentModifiers);
        combined.AddRange(equipmentModifiers);

        AppendBuffModifiers(combined, permanentActiveBuffs);
        AppendBuffModifiers(combined, runScopedActiveBuffs);
        return combined;
    }

    /// <summary>将 Buff 列表中的属性修正追加到目标集合。</summary>
    private static void AppendBuffModifiers(List<StatModifierConfig> destination, List<BuffRuntimeData> buffs)
    {
        for (int i = 0; i < buffs.Count; i++)
        {
            IReadOnlyList<StatModifierConfig> buffMods = buffs[i].Modifiers;
            if (buffMods == null)
            {
                continue;
            }

            for (int j = 0; j < buffMods.Count; j++)
            {
                if (buffMods[j] != null)
                {
                    destination.Add(buffMods[j]);
                }
            }
        }
    }

    /// <summary>推进列表内 Buff 计时并移除过期项。</summary>
    /// <returns>列表发生变化时返回 <c>true</c>。</returns>
    private static bool TickBuffList(List<BuffRuntimeData> buffs, float deltaTime)
    {
        bool changed = false;
        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            BuffRuntimeData buff = buffs[i];
            buff.Tick(deltaTime);
            if (buff.IsExpired)
            {
                buffs.RemoveAt(i);
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>从存档永久成长项恢复 Buff。</summary>
    /// <param name="save">存档数据。</param>
    private void ApplyPermanentGrowthFromSave(SaveData save)
    {
        if (save == null || save.permanentUpgrades == null || save.permanentUpgrades.Count == 0)
        {
            return;
        }

        if (!ServiceLocator.TryGet(out ConfigManager configManager))
        {
            return;
        }

        for (int i = 0; i < save.permanentUpgrades.Count; i++)
        {
            ConfigIdIntPair entry = save.permanentUpgrades[i];
            if (entry.value <= 0 || string.IsNullOrEmpty(entry.configId))
            {
                continue;
            }

            if (!configManager.TryGetBuff(entry.configId, out BuffDataSO buffData))
            {
                continue;
            }

            // 带 SkillBuff 的条目由 SkillManager 局外永久管线施加，避免与 Profile 重复叠层。
            if (buffData.HasSkillBuff)
            {
                continue;
            }

            permanentActiveBuffs.Add(ConfigRuntimeFactory.CreateBuff(buffData, entry.value));
        }
    }
}
