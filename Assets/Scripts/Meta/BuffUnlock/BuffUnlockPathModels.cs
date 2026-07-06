/// <summary>路径节点解锁状态。</summary>
public enum BuffUnlockNodeState
{
    /// <summary>已解锁。</summary>
    Unlocked,
    /// <summary>当前待解锁（首个未解锁）。</summary>
    Pending,
    /// <summary>尚未到达。</summary>
    Locked,
}

/// <summary>单条 Buff 解锁路径节点。</summary>
public readonly struct BuffUnlockPathNode
{
    public BuffUnlockPathNode(
        bool isGlobal,
        SkillType skillType,
        SkillBuffKind kind,
        int tier,
        BuffUnlockNodeState state,
        int requiredCards,
        int globalIndexInPath)
    {
        IsGlobal = isGlobal;
        SkillType = skillType;
        Kind = kind;
        Tier = tier;
        State = state;
        RequiredCards = requiredCards;
        GlobalIndexInPath = globalIndexInPath;
    }

    /// <summary>是否为通用 Buff 节点。</summary>
    public bool IsGlobal { get; }
    /// <summary>所属技能（专属节点有效）。</summary>
    public SkillType SkillType { get; }
    /// <summary>目标 Buff 种类。</summary>
    public SkillBuffKind Kind { get; }
    /// <summary>解锁后达到的 tier。</summary>
    public int Tier { get; }
    /// <summary>节点状态。</summary>
    public BuffUnlockNodeState State { get; }
    /// <summary>解锁所需解锁卡数量。</summary>
    public int RequiredCards { get; }
    /// <summary>在整条路径中的序号（0 起）。</summary>
    public int GlobalIndexInPath { get; }
}

/// <summary>单技能当前路径窗口（水平展示，不可横滑）。</summary>
public readonly struct BuffUnlockSkillPathWindow
{
    public BuffUnlockSkillPathWindow(
        SkillType skillType,
        string skillDisplayName,
        BuffUnlockPathNode[] nodes,
        bool isComplete)
    {
        SkillType = skillType;
        SkillDisplayName = skillDisplayName ?? string.Empty;
        Nodes = nodes ?? System.Array.Empty<BuffUnlockPathNode>();
        IsComplete = isComplete;
    }

    public SkillType SkillType { get; }
    public string SkillDisplayName { get; }
    public BuffUnlockPathNode[] Nodes { get; }
    public bool IsComplete { get; }
}

/// <summary>背包格子展示数据。</summary>
public readonly struct BuffUnlockInventorySlot
{
    public BuffUnlockInventorySlot(string cardConfigId, string displayName, int count, bool isGlobal, SkillType skillType)
    {
        CardConfigId = cardConfigId ?? string.Empty;
        DisplayName = displayName ?? string.Empty;
        Count = count;
        IsGlobal = isGlobal;
        SkillType = skillType;
    }

    public string CardConfigId { get; }
    public string DisplayName { get; }
    public int Count { get; }
    public bool IsGlobal { get; }
    public SkillType SkillType { get; }
}
