using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 顶部资源条（主场景 / 商城共用）：水晶、金币、广告券、体力，从左到右第 1~4 项一一对应。
/// </summary>
public class TopResourceBarPanel : MonoBehaviour
{
    [SerializeField] private UI_ItemSlot diamondSlot;
    [SerializeField] private UI_ItemSlot goldSlot;
    [FormerlySerializedAs("energySlot")]
    [SerializeField] private UI_ItemSlot adTicketSlot;
    [FormerlySerializedAs("techPointSlot")]
    [FormerlySerializedAs("ticketSlot")]
    [SerializeField] private UI_ItemSlot staminaSlot;

    public UI_ItemSlot DiamondSlot => diamondSlot;
    public UI_ItemSlot GoldSlot => goldSlot;
    public UI_ItemSlot AdTicketSlot => adTicketSlot;
    public UI_ItemSlot StaminaSlot => staminaSlot;

    public event Action<CurrencyType> AddClicked;

    private float staminaSubtitleRefreshTimer;

    private void Awake()
    {
        BindAdd(diamondSlot);
        BindAdd(goldSlot);
        BindAdd(adTicketSlot);
        BindAdd(staminaSlot);
    }

    private void OnDestroy()
    {
        UnbindAdd(diamondSlot);
        UnbindAdd(goldSlot);
        UnbindAdd(adTicketSlot);
        UnbindAdd(staminaSlot);
    }

    private void Update()
    {
        staminaSubtitleRefreshTimer += Time.unscaledDeltaTime;
        if (staminaSubtitleRefreshTimer < 1f)
        {
            return;
        }

        staminaSubtitleRefreshTimer = 0f;
        RefreshStaminaSubtitle();
    }

    public void Refresh()
    {
        long gold = 0;
        long diamonds = 0;
        int adTickets = 0;

        if (ServiceLocator.TryGet(out ResourceManager resources))
        {
            gold = resources.GetAmount(CurrencyType.Gold);
            diamonds = resources.GetAmount(CurrencyType.Diamond);
            adTickets = resources.GetAdTicketCount();
        }
        else if (ServiceLocator.TryGet(out SaveManager save) && save.Current != null)
        {
            gold = save.Current.gold;
            diamonds = save.Current.diamonds;
            adTickets = save.Current.adTickets;
        }

        diamondSlot?.SetValue(diamonds);
        goldSlot?.SetValue(gold);
        adTicketSlot?.SetValue(adTickets.ToString(CultureInfo.InvariantCulture));
        RefreshStaminaSubtitle();
    }

    private void RefreshStaminaSubtitle()
    {
        if (staminaSlot == null)
        {
            return;
        }

        if (!ServiceLocator.TryGet(out ResourceManager resources))
        {
            staminaSlot.SetValue($"0/{StaminaConstants.DefaultMaxStamina}");
            staminaSlot.SetSubValue(null);
            return;
        }

        staminaSlot.SetValue(resources.GetStaminaDisplayText());
        staminaSlot.SetSubValue(resources.GetStaminaRecoverySubtitle());
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
