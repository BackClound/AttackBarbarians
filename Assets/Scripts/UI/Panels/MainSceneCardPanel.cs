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

    public event Action<MainSceneAction> CardClicked;

    private void Awake()
    {
        RebuildCache();
        for (int i = 0; i < cardList.Count; i++)
        {
            GeneralCardPanel card = cardList[i];
            card.Clicked += OnCardClicked;
        }
    }

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

    public void SetRedDot(MainSceneAction action, bool visible)
    {
        GeneralCardPanel card = FindCard(action);
        card?.SetRedDot(visible);
    }

    public void SetBattleTabSelected(bool battleSelected)
    {
        SetNavTabSelected(battleSelected ? MainSceneAction.Battle : MainSceneAction.Shop);
    }

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

    private void OnCardClicked(MainSceneAction action)
    {
        CardClicked?.Invoke(action);
    }
}
