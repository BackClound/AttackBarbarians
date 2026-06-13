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

    /// <summary>水晶槽位 Widget。</summary>
    public UI_ItemSlot DiamondSlot => diamondSlot;
    /// <summary>金币槽位 Widget。</summary>
    public UI_ItemSlot GoldSlot => goldSlot;
    /// <summary>广告券槽位 Widget。</summary>
    public UI_ItemSlot AdTicketSlot => adTicketSlot;
    /// <summary>体力槽位 Widget。</summary>
    public UI_ItemSlot StaminaSlot => staminaSlot;

    /// <summary>任意槽位加号被点击时触发。</summary>
    public event Action<CurrencyType> AddClicked;

    private float staminaSubtitleRefreshTimer;

    /// <summary>绑定各槽位加号点击事件。</summary>
    private void Awake()
    {
        BindAdd(diamondSlot);
        BindAdd(goldSlot);
        BindAdd(adTicketSlot);
        BindAdd(staminaSlot);
    }

    /// <summary>解绑各槽位加号点击事件。</summary>
    private void OnDestroy()
    {
        UnbindAdd(diamondSlot);
        UnbindAdd(goldSlot);
        UnbindAdd(adTicketSlot);
        UnbindAdd(staminaSlot);
    }

    /// <summary>每秒刷新体力恢复副标题。</summary>
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

    /// <summary>从 <see cref="ResourceManager"/> 或存档刷新四项资源数值。</summary>
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

    /// <summary>刷新体力主值与恢复倒计时副标题。</summary>
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

    /// <summary>绑定槽位加号点击事件。</summary>
    private void BindAdd(UI_ItemSlot slot)
    {
        if (slot != null)
        {
            slot.AddClicked += OnSlotAddClicked;
        }
    }

    /// <summary>解绑槽位加号点击事件。</summary>
    private void UnbindAdd(UI_ItemSlot slot)
    {
        if (slot != null)
        {
            slot.AddClicked -= OnSlotAddClicked;
        }
    }

    /// <summary>将槽位加号点击转发为 AddClicked 事件。</summary>
    private void OnSlotAddClicked(CurrencyType currency)
    {
        AddClicked?.Invoke(currency);
    }
}
