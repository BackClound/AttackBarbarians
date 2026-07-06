using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 科技页上半区：各技能 Buff 解锁路径（占页面约 60%，纵向可滚动）。
/// </summary>
public class TechBuffUnlockPathPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text sectionTitleText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform rowHost;
    [SerializeField] private BuffUnlockSkillRowView rowPrefab;
    [SerializeField] private TMP_Text emptyHintText;

    private readonly List<BuffUnlockSkillRowView> activeRows = new List<BuffUnlockSkillRowView>(8);

    /// <summary>请求解锁某技能下一节点。</summary>
    public event Action<SkillType> UnlockRequested;

    /// <summary>刷新全部技能路径行。</summary>
    public void Refresh(IReadOnlyList<BuffUnlockSkillPathWindow> windows)
    {
        if (sectionTitleText != null)
        {
            sectionTitleText.text = "技能 Buff 解锁路线";
            sectionTitleText.color = UiTechWastelandPalette.TextPrimary;
        }

        ClearRows();
        if (windows == null || windows.Count == 0)
        {
            if (emptyHintText != null)
            {
                emptyHintText.gameObject.SetActive(true);
                emptyHintText.text = "暂无可展示的技能路径";
            }

            return;
        }

        if (emptyHintText != null)
        {
            emptyHintText.gameObject.SetActive(false);
        }

        if (rowPrefab == null || rowHost == null)
        {
            return;
        }

        for (int i = 0; i < windows.Count; i++)
        {
            BuffUnlockSkillRowView row = Instantiate(rowPrefab, rowHost);
            row.gameObject.SetActive(true);
            row.UnlockRequested += HandleUnlockRequested;
            row.SetData(windows[i]);
            activeRows.Add(row);
        }

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void OnDestroy()
    {
        ClearRows();
    }

    private void HandleUnlockRequested(SkillType skillType)
    {
        UnlockRequested?.Invoke(skillType);
    }

    private void ClearRows()
    {
        for (int i = 0; i < activeRows.Count; i++)
        {
            if (activeRows[i] != null)
            {
                activeRows[i].UnlockRequested -= HandleUnlockRequested;
                Destroy(activeRows[i].gameObject);
            }
        }

        activeRows.Clear();
    }
}
