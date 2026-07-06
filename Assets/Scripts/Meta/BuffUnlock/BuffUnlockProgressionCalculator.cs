using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 计算各技能 Buff 解锁路径：5 通用 → 1 专属 → 7 通用 → 1 专属…
/// </summary>
public static class BuffUnlockProgressionCalculator
{
    /// <summary>构建当前水平窗口（整段区块，不可横滑）。</summary>
    public static BuffUnlockSkillPathWindow BuildWindow(
        SkillType skillType,
        string skillDisplayName,
        int unlockedNodeCount)
    {
        if (!TryGetSegmentRange(unlockedNodeCount, out int segmentStart, out int segmentEnd, out bool isComplete))
        {
            return new BuffUnlockSkillPathWindow(skillType, skillDisplayName, System.Array.Empty<BuffUnlockPathNode>(), true);
        }

        int pendingIndex = isComplete ? -1 : unlockedNodeCount;
        var nodes = new List<BuffUnlockPathNode>(segmentEnd - segmentStart);
        for (int i = segmentStart; i < segmentEnd; i++)
        {
            if (!TryResolveNodeAtIndex(skillType, i, out bool isGlobal, out SkillBuffKind kind, out int tier))
            {
                break;
            }

            BuffUnlockNodeState state = i < unlockedNodeCount
                ? BuffUnlockNodeState.Unlocked
                : i == pendingIndex
                    ? BuffUnlockNodeState.Pending
                    : BuffUnlockNodeState.Locked;

            nodes.Add(new BuffUnlockPathNode(
                isGlobal,
                skillType,
                kind,
                tier,
                state,
                GetRequiredCardCount(i),
                i));
        }

        return new BuffUnlockSkillPathWindow(skillType, skillDisplayName, nodes.ToArray(), isComplete);
    }

    /// <summary>整条路径是否已全部解锁。</summary>
    public static bool IsPathComplete(SkillType skillType, int unlockedNodeCount)
    {
        return TryResolveNodeAtIndex(skillType, unlockedNodeCount, out _, out _, out _) == false;
    }

    /// <summary>解锁指定序号节点所需卡数。</summary>
    public static int GetRequiredCardCount(int nodeIndex) => 1 + nodeIndex / 4;

    /// <summary>解析路径上某一节点的 Buff 定义。</summary>
    public static bool TryResolveNodeAtIndex(
        SkillType skillType,
        int nodeIndex,
        out bool isGlobal,
        out SkillBuffKind kind,
        out int tier)
    {
        isGlobal = false;
        kind = SkillBuffKind.None;
        tier = 0;

        int blockIndex = 0;
        int consumed = 0;
        while (true)
        {
            bool exclusiveBlock = blockIndex % 2 == 1;
            int blockSize = exclusiveBlock
                ? 1
                : BuffUnlockSkillDefinitions.GetGlobalBlockSize(blockIndex / 2);

            if (nodeIndex < consumed + blockSize)
            {
                int offset = nodeIndex - consumed;
                if (exclusiveBlock)
                {
                    isGlobal = false;
                    int exclusiveIndex = blockIndex / 2;
                    IReadOnlyList<SkillBuffKind> exclusives = BuffUnlockSkillDefinitions.GetExclusiveKinds(skillType);
                    if (exclusiveIndex >= exclusives.Count)
                    {
                        return false;
                    }

                    kind = exclusives[exclusiveIndex];
                    tier = 1;
                    return true;
                }

                isGlobal = true;
                IReadOnlyList<SkillBuffKind> globals = BuffUnlockSkillDefinitions.GlobalKindsList;
                kind = globals[offset % globals.Count];
                tier = CountKindOccurrencesBeforeIndex(skillType, nodeIndex, kind) + 1;
                return true;
            }

            consumed += blockSize;
            blockIndex++;

            if (blockIndex > 256)
            {
                return false;
            }
        }
    }

    /// <summary>当前应展示的路径段起止（含 end，不含）。</summary>
    private static bool TryGetSegmentRange(
        int unlockedNodeCount,
        out int segmentStart,
        out int segmentEnd,
        out bool isComplete)
    {
        segmentStart = 0;
        segmentEnd = 0;
        isComplete = false;

        int blockIndex = 0;
        int consumed = 0;
        while (blockIndex < 256)
        {
            bool exclusiveBlock = blockIndex % 2 == 1;
            int blockSize = exclusiveBlock
                ? 1
                : BuffUnlockSkillDefinitions.GetGlobalBlockSize(blockIndex / 2);
            int blockEnd = consumed + blockSize;

            if (unlockedNodeCount < blockEnd)
            {
                segmentStart = consumed;
                segmentEnd = blockEnd;
                return true;
            }

            consumed = blockEnd;
            blockIndex++;
        }

        isComplete = true;
        return false;
    }

    private static int CountKindOccurrencesBeforeIndex(SkillType skillType, int nodeIndex, SkillBuffKind targetKind)
    {
        int count = 0;
        for (int i = 0; i < nodeIndex; i++)
        {
            if (!TryResolveNodeAtIndex(skillType, i, out bool isGlobal, out SkillBuffKind kind, out _) ||
                !isGlobal ||
                kind != targetKind)
            {
                continue;
            }

            count++;
        }

        return count;
    }
}
