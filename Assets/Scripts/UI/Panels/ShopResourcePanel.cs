using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// 商城顶部资源条：水晶、金币、体力、科技点。
/// </summary>
public class ShopResourcePanel : MonoBehaviour
{
    [SerializeField] private UI_ItemSlot diamondSlot;
    [SerializeField] private UI_ItemSlot goldSlot;
    [SerializeField] private UI_ItemSlot energySlot;
    [SerializeField] private UI_ItemSlot techPointSlot;

    public event Action<CurrencyType> AddClicked;

    private void Awake()
    {
        BindAdd(diamondSlot);
        BindAdd(goldSlot);
        BindAdd(energySlot);
        BindAdd(techPointSlot);
    }

    private void OnDestroy()
    {
        UnbindAdd(diamondSlot);
        UnbindAdd(goldSlot);
        UnbindAdd(energySlot);
        UnbindAdd(techPointSlot);
    }

    public void Refresh()
    {
        long gold = 0;
        long diamonds = 0;
        long techPoints = 0;
        int adTickets = 0;

        string energyText = $"{EnergyConstants.DefaultStartingEnergy}/{EnergyConstants.DefaultMaxEnergy}";
        if (ServiceLocator.TryGet(out ResourceManager resources))
        {
            gold = resources.GetAmount(CurrencyType.Gold);
            diamonds = resources.GetAmount(CurrencyType.Diamond);
            energyText = resources.GetEnergyDisplayText();
        }
        else if (ServiceLocator.TryGet(out SaveManager save) && save.Current != null)
        {
            gold = save.Current.gold;
            diamonds = save.Current.diamonds;
            energyText = $"{save.Current.energy}/{save.Current.maxEnergy}";
        }

        if (ServiceLocator.TryGet(out ShopManager shop))
        {
            techPoints = shop.GetTechPoints();
            adTickets = shop.GetAdTicketCount();
        }

        diamondSlot?.SetValue(diamonds);
        goldSlot?.SetValue(gold);
        energySlot?.SetValue(energyText);

        techPointSlot?.SetValue(techPoints.ToString("N0", CultureInfo.InvariantCulture));
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
