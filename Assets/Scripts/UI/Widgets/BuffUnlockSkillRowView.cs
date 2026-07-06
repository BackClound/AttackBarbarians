using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 单技能 Buff 解锁路径行：技能名 + 水平不可滑动节点带。
/// </summary>
public class BuffUnlockSkillRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private Transform nodeHost;
    [SerializeField] private BuffUnlockPathNodeView nodePrefab;

    private readonly List<BuffUnlockPathNodeView> activeNodes = new List<BuffUnlockPathNodeView>(12);

    /// <summary>请求解锁下一节点。</summary>
    public event Action<SkillType> UnlockRequested;

    /// <summary>刷新整行路径窗口。</summary>
    public void SetData(BuffUnlockSkillPathWindow window)
    {
        if (skillNameText != null)
        {
            skillNameText.text = window.IsComplete
                ? $"{window.SkillDisplayName} · 路径已完成"
                : window.SkillDisplayName;
            skillNameText.color = UiTechWastelandPalette.TextPrimary;
        }

        ClearNodes();
        BuffUnlockPathNode[] nodes = window.Nodes;
        if (nodes == null || nodePrefab == null || nodeHost == null)
        {
            return;
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            BuffUnlockPathNodeView view = Instantiate(nodePrefab, nodeHost);
            view.gameObject.SetActive(true);
            view.UnlockRequested += HandleUnlockRequested;
            view.SetData(nodes[i]);
            activeNodes.Add(view);
        }
    }

    private void OnDestroy()
    {
        ClearNodes();
    }

    private void HandleUnlockRequested(SkillType skillType)
    {
        UnlockRequested?.Invoke(skillType);
    }

    private void ClearNodes()
    {
        for (int i = 0; i < activeNodes.Count; i++)
        {
            if (activeNodes[i] != null)
            {
                activeNodes[i].UnlockRequested -= HandleUnlockRequested;
                Destroy(activeNodes[i].gameObject);
            }
        }

        activeNodes.Clear();
    }
}
