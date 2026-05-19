using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 伤害飘字控制器：订阅 <see cref="GameConstants.EventKeys.DamageApplied"/> 显示数字。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 UI Canvas 或专用 <c>DamageNumberRoot</c> 物体上。</para>
/// <para><b>Inspector：</b>配置 <c>numberCanvas</c>、<c>numberPrefab</c>。</para>
/// <para><b>兼容：</b><see cref="ShowDamageNumber"/> 仍可供调试或旧代码旁路调用；敌人受击已改由事件驱动。</para>
/// </remarks>
public class DamageNumberController : GameEventSubscriberBase
{
    /// <summary>旧字段名，与 <see cref="Instance"/> 相同。</summary>
    public static DamageNumberController numberControllerInstance => Instance;

    public static DamageNumberController Instance => SingletonHost<DamageNumberController>.Instance;

    public static bool HasInstance => SingletonHost<DamageNumberController>.HasInstance;

    [SerializeField] private Transform numberCanvas;
    [SerializeField] private GameObject numberPrefab;

    private readonly List<DamageNumber> damageNumbers = new List<DamageNumber>();
    private readonly ConcurrentQueue<DamageNumber> availableDamageNumbers = new ConcurrentQueue<DamageNumber>();

    private void Awake()
    {
        SingletonHost<DamageNumberController>.TryClaim(this, this, SingletonOptions.SceneDefault, out _);
    }

    private void OnDestroy()
    {
        SingletonHost<DamageNumberController>.Release(this);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ShowDamageNumber(10, Vector3.one);
        }
    }

    protected override void RegisterHandlers(EventBus bus)
    {
        GameEvents.SubscribeDamageApplied(OnDamageApplied);
    }

    protected override void UnregisterHandlers(EventBus bus)
    {
        GameEvents.UnsubscribeDamageApplied(OnDamageApplied);
    }

    private void OnDamageApplied(GameEventContext context)
    {
        if (context.Payload is not DamageEventArgs args)
        {
            return;
        }

        ShowDamageNumber(args.Amount, args.WorldPosition);
    }

    public void ShowDamageNumber(float totalDamage, Vector3 location)
    {
        DamageNumber number = GetDamageNumber();
        number?.SetupNumber(totalDamage, location);
    }

    private DamageNumber GetDamageNumber()
    {
        if (availableDamageNumbers.Count == 0)
        {
            DamageNumber newNumber = InitialDamageNumber();
            if (newNumber != null && !damageNumbers.Contains(newNumber))
            {
                damageNumbers.Add(newNumber);
                availableDamageNumbers.Enqueue(newNumber);
            }
        }

        return availableDamageNumbers.TryDequeue(out DamageNumber damageNumber) ? damageNumber : null;
    }

    private DamageNumber InitialDamageNumber()
    {
        var number = Instantiate(numberPrefab, transform.position, Quaternion.identity, numberCanvas);
        number.SetActive(false);
        return number.GetComponent<DamageNumber>();
    }
}
