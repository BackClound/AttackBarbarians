using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 伤害飘字控制器：订阅 <see cref="GameConstants.EventKeys.DamageApplied"/> 显示数字。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 UI Canvas 或专用 <c>DamageNumberRoot</c> 物体上。</para>
/// <para><b>Inspector：</b>配置 <c>numberCanvas</c>、<c>numberPrefab</c>；优先从 <see cref="PoolManager"/> 获取（Key = <see cref="GameConstants.PoolKeys.DamageNumber"/>）。</para>
/// <para><b>兼容：</b>未配置对象池时回退到本地队列 + Instantiate。</para>
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
    private readonly Queue<DamageNumber> availableDamageNumbers = new Queue<DamageNumber>();

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
        if (ServiceLocator.TryGet(out PoolManager poolManager) && poolManager.HasPool(GameConstants.PoolKeys.DamageNumber))
        {
            DamageNumber pooled = poolManager.Spawn<DamageNumber>(
                GameConstants.PoolKeys.DamageNumber,
                Vector3.zero,
                Quaternion.identity,
                numberCanvas);
            return pooled;
        }

        if (availableDamageNumbers.Count == 0)
        {
            DamageNumber newNumber = CreateFallbackDamageNumber();
            if (newNumber != null && !damageNumbers.Contains(newNumber))
            {
                damageNumbers.Add(newNumber);
                availableDamageNumbers.Enqueue(newNumber);
            }
        }

        return availableDamageNumbers.Count > 0 && availableDamageNumbers.TryDequeue(out DamageNumber damageNumber)
            ? damageNumber
            : null;
    }

    private DamageNumber CreateFallbackDamageNumber()
    {
        if (numberPrefab == null || numberCanvas == null)
        {
            return null;
        }

        GameObject instance = Instantiate(numberPrefab, numberCanvas.position, Quaternion.identity, numberCanvas);
        instance.SetActive(false);
        return instance.GetComponent<DamageNumber>();
    }
}
