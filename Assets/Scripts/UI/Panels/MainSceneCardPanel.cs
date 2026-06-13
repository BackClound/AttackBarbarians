using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MainScene 图标按钮区域：聚合多个 <see cref="GeneralCardPanel"/> 并转发点击。
/// </summary>
public class MainSceneCardPanel : MonoBehaviour
{
    [SerializeField] private GeneralCardPanel[] cards;

    private readonly List<GeneralCardPanel> cardList = new List<GeneralCardPanel>(32);

    /// <summary>区域内任意卡片被点击时触发。</summary>
    public event Action<MainSceneAction> CardClicked;

    /// <summary>重建卡片缓存并绑定各 Widget 点击事件。</summary>
    private void Awake()
    {
        RebuildCache();
        for (int i = 0; i < cardList.Count; i++)
        {
            GeneralCardPanel card = cardList[i];
            card.Clicked += OnCardClicked;
        }
    }

    /// <summary>解绑所有卡片点击事件。</summary>
    private void OnDestroy()
    {
        for (int i = 0; i < cardList.Count; i++)
        {
            if (cardList[i] != null)
            {
                cardList[i].Clicked -= OnCardClicked;
            }
        }
    }

    /// <summary>按动作标识查找对应卡片 Widget。</summary>
    /// <param name="action">目标动作。</param>
    /// <returns>匹配的 <see cref="GeneralCardPanel"/>，未找到返回 <c>null</c>。</returns>
    public GeneralCardPanel FindCard(MainSceneAction action)
    {
        for (int i = 0; i < cardList.Count; i++)
        {
            if (cardList[i] != null && cardList[i].Action == action)
            {
                return cardList[i];
            }
        }

        return null;
    }

    /// <summary>为指定入口设置红点状态。</summary>
    /// <param name="action">目标动作。</param>
    /// <param name="visible">是否显示红点。</param>
    public void SetRedDot(MainSceneAction action, bool visible)
    {
        GeneralCardPanel card = FindCard(action);
        card?.SetRedDot(visible);
    }

    /// <summary>设置战斗/商城底栏选中态（兼容旧 API）。</summary>
    /// <param name="battleSelected">为 <c>true</c> 时选中战斗页。</param>
    public void SetBattleTabSelected(bool battleSelected)
    {
        SetNavTabSelected(battleSelected ? MainSceneAction.Battle : MainSceneAction.Shop);
    }

    /// <summary>设置底栏导航选中高亮。</summary>
    /// <param name="selectedAction">当前选中的底栏动作。</param>
    public void SetNavTabSelected(MainSceneAction selectedAction)
    {
        for (int i = 0; i < cardList.Count; i++)
        {
            GeneralCardPanel card = cardList[i];
            if (card != null)
            {
                card.SetSelectedHighlight(card.Action == selectedAction);
            }
        }
    }

    /// <summary>从序列化数组重建有效卡片列表。</summary>
    private void RebuildCache()
    {
        cardList.Clear();
        if (cards == null)
        {
            return;
        }

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] != null)
            {
                cardList.Add(cards[i]);
            }
        }
    }

    /// <summary>将 Widget 点击转发为卡片动作事件。</summary>
    private void OnCardClicked(MainSceneAction action)
    {
        CardClicked?.Invoke(action);
    }
}
