using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 局外 Buff 解锁 Meta 服务：路径进度、解锁卡库存与永久 Buff 写入。
/// </summary>
public class BuffUnlockService : MonoBehaviour, IGameSystem
{
    private SaveManager saveManager;
    private ConfigManager configManager;
    private BuffMetaProgressCatalog buffCatalog;
    private bool isInitialized;

    /// <summary>是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;

    /// <summary>初始化依赖。</summary>
    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        ServiceLocator.TryGet(out saveManager);
        ServiceLocator.TryGet(out configManager);
        buffCatalog = BuffMetaProgressCatalog.LoadDefault();
        isInitialized = true;
    }

    /// <summary>无逐帧逻辑。</summary>
    public void Tick(float deltaTime) { }

    /// <summary>重置初始化状态。</summary>
    public void Shutdown() => isInitialized = false;

    /// <summary>读取解锁卡库存。</summary>
    public int GetCardCount(string cardConfigId)
    {
        SaveData save = saveManager?.Current;
        return save == null || string.IsNullOrWhiteSpace(cardConfigId)
            ? 0
            : save.GetBuffUnlockCardCount(cardConfigId);
    }

    /// <summary>增加解锁卡。</summary>
    public bool TryAddCard(string cardConfigId, int count)
    {
        if (!isInitialized || count <= 0 || string.IsNullOrWhiteSpace(cardConfigId) || saveManager?.Current == null)
        {
            return false;
        }

        int current = GetCardCount(cardConfigId);
        saveManager.Current.SetBuffUnlockCardCount(cardConfigId, current + count);
        saveManager.MarkDirty();
        GameEvents.RaiseBuffUnlockChanged(this, null);
        return true;
    }

    /// <summary>读取技能路径已解锁节点数。</summary>
    public int GetUnlockedNodeCount(SkillType skillType)
    {
        SaveData save = saveManager?.Current;
        string skillId = MetaSkillBuffProgressResolver.ResolveSkillConfigId(skillType);
        return save == null || string.IsNullOrWhiteSpace(skillId)
            ? 0
            : save.GetSkillBuffUnlockProgress(skillId);
    }

    /// <summary>构建所有可展示技能的路径窗口。</summary>
    public List<BuffUnlockSkillPathWindow> BuildAllSkillWindows()
    {
        var result = new List<BuffUnlockSkillPathWindow>(8);
        SkillType[] order = BuffUnlockSkillDefinitions.DisplayOrder;
        for (int i = 0; i < order.Length; i++)
        {
            SkillType skillType = order[i];
            if (!IsSkillVisible(skillType))
            {
                continue;
            }

            result.Add(BuildSkillWindow(skillType));
        }

        return result;
    }

    /// <summary>构建单技能路径窗口。</summary>
    public BuffUnlockSkillPathWindow BuildSkillWindow(SkillType skillType)
    {
        string displayName = ResolveSkillDisplayName(skillType);
        int progress = GetUnlockedNodeCount(skillType);
        return BuffUnlockProgressionCalculator.BuildWindow(skillType, displayName, progress);
    }

    /// <summary>构建背包格子（通用卡在前，按技能分组）。</summary>
    public List<BuffUnlockInventorySlot> BuildInventorySlots()
    {
        var slots = new List<BuffUnlockInventorySlot>(16);
        int globalCount = GetCardCount(BuffUnlockCardConstants.Global);
        if (globalCount > 0)
        {
            slots.Add(new BuffUnlockInventorySlot(
                BuffUnlockCardConstants.Global,
                BuffUnlockCardConstants.GlobalDisplayName,
                globalCount,
                true,
                SkillType.None));
        }

        SkillType[] order = BuffUnlockSkillDefinitions.DisplayOrder;
        for (int i = 0; i < order.Length; i++)
        {
            SkillType skillType = order[i];
            string cardId = BuffUnlockCardConstants.GetSkillCardId(skillType);
            if (string.IsNullOrWhiteSpace(cardId))
            {
                continue;
            }

            int count = GetCardCount(cardId);
            if (count <= 0)
            {
                continue;
            }

            slots.Add(new BuffUnlockInventorySlot(
                cardId,
                BuffUnlockPresentationResolver.ResolveCardDisplayName(cardId, skillType),
                count,
                false,
                skillType));
        }

        return slots;
    }

    /// <summary>尝试解锁技能路径上下一个待解锁节点。</summary>
    public bool TryUnlockNext(SkillType skillType, out string message)
    {
        message = string.Empty;
        if (!isInitialized || saveManager?.Current == null)
        {
            message = "存档未就绪";
            return false;
        }

        int progress = GetUnlockedNodeCount(skillType);
        if (BuffUnlockProgressionCalculator.IsPathComplete(skillType, progress))
        {
            message = "该技能 Buff 路径已全部解锁";
            return false;
        }

        if (!BuffUnlockProgressionCalculator.TryResolveNodeAtIndex(
                skillType,
                progress,
                out bool isGlobal,
                out SkillBuffKind kind,
                out int tier))
        {
            message = "路径已结束";
            return false;
        }

        int required = BuffUnlockProgressionCalculator.GetRequiredCardCount(progress);
        string cardId = isGlobal
            ? BuffUnlockCardConstants.Global
            : BuffUnlockCardConstants.GetSkillCardId(skillType);

        if (GetCardCount(cardId) < required)
        {
            message = $"解锁卡不足（需要 {required} 张）";
            return false;
        }

        if (!TryConsumeCards(cardId, required))
        {
            message = "扣除解锁卡失败";
            return false;
        }

        ApplyPermanentBuff(kind, tier);
        string skillConfigId = MetaSkillBuffProgressResolver.ResolveSkillConfigId(skillType);
        saveManager.Current.SetSkillBuffUnlockProgress(skillConfigId, progress + 1);
        saveManager.MarkDirty();
        saveManager.SaveImmediate();

        GameEvents.RaiseBuffUnlockChanged(this, new BuffUnlockChangedEventArgs(skillType, kind, tier));
        var unlockedNode = new BuffUnlockPathNode(
            isGlobal, skillType, kind, tier, BuffUnlockNodeState.Unlocked, required, progress);
        message = $"已解锁：{BuffUnlockPresentationResolver.ResolveNodeTitle(unlockedNode)}";
        return true;
    }

    private bool TryConsumeCards(string cardConfigId, int count)
    {
        int current = GetCardCount(cardConfigId);
        if (current < count)
        {
            return false;
        }

        saveManager.Current.SetBuffUnlockCardCount(cardConfigId, current - count);
        return true;
    }

    private void ApplyPermanentBuff(SkillBuffKind kind, int tier)
    {
        SaveData save = saveManager.Current;
        if (buffCatalog == null || save == null)
        {
            return;
        }

        int currentTier = buffCatalog.GetHighestPermanentTier(save, kind);
        if (tier > currentTier)
        {
            buffCatalog.SetHighestPermanentTier(save, kind, tier);
        }
    }

    private bool IsSkillVisible(SkillType skillType)
    {
        if (skillType == SkillType.Shoot)
        {
            return true;
        }

        string skillId = MetaSkillBuffProgressResolver.ResolveSkillConfigId(skillType);
        SaveData save = saveManager?.Current;
        if (save != null && save.IsSkillUnlocked(skillId))
        {
            return true;
        }

        if (ServiceLocator.TryGet(out SkillUnlockService unlockService))
        {
            return unlockService.IsMetaUnlocked(skillId);
        }

        return false;
    }

    private string ResolveSkillDisplayName(SkillType skillType)
    {
        string skillId = MetaSkillBuffProgressResolver.ResolveSkillConfigId(skillType);
        if (!string.IsNullOrWhiteSpace(skillId) &&
            configManager != null &&
            configManager.TryGetSkill(skillId, out SkillDataSO skill) &&
            !string.IsNullOrWhiteSpace(skill.DisplayName))
        {
            return skill.DisplayName;
        }

        return skillType.ToString();
    }
}

/// <summary>Buff 解锁变更事件参数。</summary>
public sealed class BuffUnlockChangedEventArgs
{
    public BuffUnlockChangedEventArgs(SkillType skillType, SkillBuffKind kind, int tier)
    {
        SkillType = skillType;
        Kind = kind;
        Tier = tier;
    }

    public SkillType SkillType { get; }
    public SkillBuffKind Kind { get; }
    public int Tier { get; }
}
