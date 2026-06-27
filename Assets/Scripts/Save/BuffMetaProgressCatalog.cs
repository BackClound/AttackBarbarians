using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 永久成长 Buff 配置索引：按 <see cref="SkillBuffKind"/> + tier 或 StatBuff configId 解析存档条目。
/// 供 Meta 测试工具与 Editor 窗口读写 <see cref="SaveData.permanentUpgrades"/>。
/// </summary>
public sealed class BuffMetaProgressCatalog
{
    /// <summary>非 SkillBuff 的通用属性 Buff 条目。</summary>
    public readonly struct StatBuffEntry
    {
        public StatBuffEntry(string configId, string displayName)
        {
            ConfigId = configId;
            DisplayName = displayName;
        }

        public string ConfigId { get; }
        public string DisplayName { get; }
    }

    private readonly Dictionary<SkillBuffKind, SortedDictionary<int, string>> tierConfigIdsByKind =
        new Dictionary<SkillBuffKind, SortedDictionary<int, string>>(64);

    private readonly Dictionary<string, SkillBuffKind> kindByConfigId =
        new Dictionary<string, SkillBuffKind>(128);

    private readonly Dictionary<string, int> tierByConfigId =
        new Dictionary<string, int>(128);

    private readonly List<StatBuffEntry> statBuffEntries = new List<StatBuffEntry>(8);

    /// <summary>全部带 tier 的 SkillBuffKind（含全局与专属）。</summary>
    public IReadOnlyList<SkillBuffKind> SkillBuffKinds { get; private set; } = System.Array.Empty<SkillBuffKind>();

    /// <summary>通用 StatBuff 列表（不含 SkillBuff）。</summary>
    public IReadOnlyList<StatBuffEntry> StatBuffs => statBuffEntries;

    /// <summary>从 Resources 加载 Buff 资产并构建索引。</summary>
    public static BuffMetaProgressCatalog LoadDefault()
    {
        var catalog = new BuffMetaProgressCatalog();
        BuffDataSO[] buffs = Resources.LoadAll<BuffDataSO>("Config/Buff");
        catalog.BuildFrom(buffs);
        return catalog;
    }

    /// <summary>某 Kind 在配置中的最高 tier（无配置时为 0）。</summary>
    public int GetMaxTier(SkillBuffKind kind)
    {
        if (!tierConfigIdsByKind.TryGetValue(kind, out SortedDictionary<int, string> tiers) ||
            tiers.Count == 0)
        {
            return 0;
        }

        int max = 0;
        foreach (int tier in tiers.Keys)
        {
            if (tier > max)
            {
                max = tier;
            }
        }

        return max;
    }

    /// <summary>读取存档中某 Kind 已达最高永久 tier。</summary>
    public int GetHighestPermanentTier(SaveData save, SkillBuffKind kind)
    {
        if (save?.permanentUpgrades == null || kind == SkillBuffKind.None)
        {
            return 0;
        }

        int highest = 0;
        for (int i = 0; i < save.permanentUpgrades.Count; i++)
        {
            ConfigIdIntPair entry = save.permanentUpgrades[i];
            if (entry.value <= 0 || string.IsNullOrWhiteSpace(entry.configId))
            {
                continue;
            }

            if (TryResolveSkillBuff(entry.configId, out SkillBuffKind resolvedKind, out int tier) &&
                resolvedKind == kind &&
                tier > highest)
            {
                highest = tier;
            }
        }

        return highest;
    }

    /// <summary>写入某 Kind 的永久最高 tier（0 表示清除）。</summary>
    public void SetHighestPermanentTier(SaveData save, SkillBuffKind kind, int tier)
    {
        if (save == null || kind == SkillBuffKind.None)
        {
            return;
        }

        save.permanentUpgrades ??= new List<ConfigIdIntPair>(16);
        RemovePermanentEntriesForKind(save, kind);

        if (tier <= 0)
        {
            return;
        }

        if (!TryGetConfigId(kind, tier, out string configId))
        {
            Debug.LogWarning($"[BuffMetaProgressCatalog] 未找到 {kind} tier {tier} 的 Buff 配置。");
            return;
        }

        ConfigIdIntPairListUtility.SetValue(save.permanentUpgrades, configId, 1);
    }

    /// <summary>读取 StatBuff 永久层数。</summary>
    public static int GetStatBuffStacks(SaveData save, string buffConfigId) =>
        save != null ? save.GetPermanentUpgradeLevel(buffConfigId) : 0;

    /// <summary>写入 StatBuff 永久层数（0 移除）。</summary>
    public static void SetStatBuffStacks(SaveData save, string buffConfigId, int stacks)
    {
        if (save == null || string.IsNullOrWhiteSpace(buffConfigId))
        {
            return;
        }

        save.permanentUpgrades ??= new List<ConfigIdIntPair>(16);
        save.SetPermanentUpgradeLevel(buffConfigId, Mathf.Max(0, stacks));
    }

    /// <summary>解析 configId 对应的 SkillBuffKind 与 tier。</summary>
    public bool TryResolveSkillBuff(string configId, out SkillBuffKind kind, out int tier)
    {
        kind = SkillBuffKind.None;
        tier = 0;
        if (string.IsNullOrWhiteSpace(configId))
        {
            return false;
        }

        if (kindByConfigId.TryGetValue(configId, out kind))
        {
            tierByConfigId.TryGetValue(configId, out tier);
            return kind != SkillBuffKind.None;
        }

        return false;
    }

    /// <summary>查找 Kind + tier 对应的 Buff configId。</summary>
    public bool TryGetConfigId(SkillBuffKind kind, int tier, out string configId)
    {
        configId = null;
        if (kind == SkillBuffKind.None || tier <= 0)
        {
            return false;
        }

        return tierConfigIdsByKind.TryGetValue(kind, out SortedDictionary<int, string> tiers) &&
               tiers.TryGetValue(tier, out configId);
    }

    /// <summary>是否为全局 SkillBuffKind。</summary>
    public static bool IsGlobalKind(SkillBuffKind kind) => SkillBuffCatalog.IsGlobalKind(kind);

    /// <summary>获取 Kind 所属技能（全局 Kind 返回 None）。</summary>
    public static SkillType GetTargetSkill(SkillBuffKind kind) => SkillBuffCatalog.GetTargetSkill(kind);

    private void BuildFrom(BuffDataSO[] buffs)
    {
        tierConfigIdsByKind.Clear();
        kindByConfigId.Clear();
        tierByConfigId.Clear();
        statBuffEntries.Clear();

        if (buffs == null)
        {
            SkillBuffKinds = System.Array.Empty<SkillBuffKind>();
            return;
        }

        var kindSet = new HashSet<SkillBuffKind>();
        for (int i = 0; i < buffs.Length; i++)
        {
            BuffDataSO buff = buffs[i];
            if (buff == null || string.IsNullOrWhiteSpace(buff.ConfigId))
            {
                continue;
            }

            if (buff.HasSkillBuff)
            {
                SkillBuffKind kind = buff.SkillBuffKind;
                int tier = buff.SkillBuffTier;
                if (kind == SkillBuffKind.None || tier <= 0)
                {
                    continue;
                }

                if (!tierConfigIdsByKind.TryGetValue(kind, out SortedDictionary<int, string> tiers))
                {
                    tiers = new SortedDictionary<int, string>();
                    tierConfigIdsByKind[kind] = tiers;
                }

                tiers[tier] = buff.ConfigId;
                kindByConfigId[buff.ConfigId] = kind;
                tierByConfigId[buff.ConfigId] = tier;
                kindSet.Add(kind);
                continue;
            }

            if (buff.Modifiers != null && buff.Modifiers.Count > 0)
            {
                statBuffEntries.Add(new StatBuffEntry(
                    buff.ConfigId,
                    string.IsNullOrWhiteSpace(buff.DisplayName) ? buff.ConfigId : buff.DisplayName));
            }
        }

        statBuffEntries.Sort((a, b) => string.Compare(a.ConfigId, b.ConfigId, System.StringComparison.Ordinal));
        var kindList = new List<SkillBuffKind>(kindSet);
        kindList.Sort((a, b) => a.CompareTo(b));
        SkillBuffKinds = kindList;
    }

    private void RemovePermanentEntriesForKind(SaveData save, SkillBuffKind kind)
    {
        List<ConfigIdIntPair> list = save.permanentUpgrades;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            ConfigIdIntPair entry = list[i];
            if (TryResolveSkillBuff(entry.configId, out SkillBuffKind resolvedKind, out _) &&
                resolvedKind == kind)
            {
                list.RemoveAt(i);
            }
        }
    }
}
