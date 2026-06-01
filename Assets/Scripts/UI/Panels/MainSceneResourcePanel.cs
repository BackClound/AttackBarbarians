using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// MainScene 顶部资源条：钻石、金币、体力、票券。
/// </summary>
public class MainSceneResourcePanel : MonoBehaviour
{
    [SerializeField] private UI_ItemSlot diamondSlot;
    [SerializeField] private UI_ItemSlot goldSlot;
    [SerializeField] private UI_ItemSlot energySlot;
    [SerializeField] private UI_ItemSlot ticketSlot;

    [Header("Fallback")]
    [SerializeField] private int currentEnergyFallback = 30;
    [SerializeField] private int maxEnergyFallback = 30;
    [SerializeField] private int ticketFallback = 85;

    public event Action<CurrencyType> AddClicked;

    private void Awake()
    {
        BindAdd(diamondSlot);
        BindAdd(goldSlot);
        BindAdd(energySlot);
        BindAdd(ticketSlot);
    }

    private void OnDestroy()
    {
        UnbindAdd(diamondSlot);
        UnbindAdd(goldSlot);
        UnbindAdd(energySlot);
        UnbindAdd(ticketSlot);
    }

    public void Refresh()
    {
        long gold = 0;
        long diamonds = 0;
        if (ServiceLocator.TryGet(out ResourceManager resources))
        {
            gold = resources.GetAmount(CurrencyType.Gold);
            diamonds = resources.GetAmount(CurrencyType.Diamond);
        }
        else if (ServiceLocator.TryGet(out SaveManager save) && save.Current != null)
        {
            gold = save.Current.gold;
            diamonds = save.Current.diamonds;
        }

        diamondSlot?.SetValue(diamonds);
        goldSlot?.SetValue(gold);
        energySlot?.SetValue($"{currentEnergyFallback}/{maxEnergyFallback}");
        ticketSlot?.SetValue(ticketFallback.ToString(CultureInfo.InvariantCulture));
    }

    private void BindAdd(UI_ItemSlot slot)
    {
        if (slot != null)
        {
            slot.AddClicked += OnSlotAddClicked;
        }
    }

    private void UnbindAdd(UI_ItemSlot slot)
    {
        if (slot != null)
        {
            slot.AddClicked -= OnSlotAddClicked;
        }
    }

    private void OnSlotAddClicked(CurrencyType currency)
    {
        AddClicked?.Invoke(currency);
    }
}
