using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 伤害飘字视图：对象池实例，负责数值展示、上浮缩放动画与回收。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在飘字 Prefab 根物体上，由 <see cref="DamageNumberController"/> 生成。</para>
/// </remarks>
public class DamageNumber : MonoBehaviour, IPoolable
{
    /// <summary>非对象池模式下的回收回调，供 <see cref="DamageNumberController"/> 回收入队。</summary>
    public static Action<GameObject> resetNumberText;

    [SerializeField] private TextMeshProUGUI numberText;
    private int damageValue;
    [SerializeField] private float intervalThreshold;
    [SerializeField] private float intervalTime;
    [SerializeField] private float floatSpeed;
    [SerializeField] private float scaleSpeed;
    private Vector3 originalScale;

    /// <summary>缓存 TMP 引用并初始化存活计时器。</summary>
    private void Awake()
    {
        if (numberText == null)
        {
            numberText = GetComponent<TextMeshProUGUI>();
        }

        intervalTime = intervalThreshold;
    }

    /// <summary>记录初始缩放，用于回池时还原。</summary>
    private void Start()
    {
        originalScale = transform.localScale;
    }

    /// <summary>每帧上浮、放大，超时后触发回收。</summary>
    void Update()
    {
        intervalTime -= Time.deltaTime;
        if (intervalTime < 0)
        {
            DisableNumberComponent();
            return;
        }
        transform.position = transform.position + Vector3.up * floatSpeed * Time.deltaTime;
        transform.localScale += Vector3.one * scaleSpeed * Time.deltaTime;
    }

    /// <summary>配置飘字数值与世界坐标并激活显示。</summary>
    /// <param name="totalDamage">伤害总量，四舍五入为整数显示。</param>
    /// <param name="location">屏幕或世界空间中的显示位置。</param>
    public void SetupNumber(float totalDamage, Vector2 location)
    {
        if (numberText != null)
        {
            var damage = Mathf.RoundToInt(totalDamage);
            numberText.text = damage.ToString();
            transform.position = location;
            intervalTime = intervalThreshold;
            gameObject.SetActive(true);
        }
    }

    /// <summary>对象池取出时重置存活计时器。</summary>
    public void OnSpawn()
    {
        intervalTime = intervalThreshold;
    }

    /// <summary>对象池归还时清空文本、数值与缩放。</summary>
    public void OnDespawn()
    {
        if (numberText != null)
        {
            numberText.text = string.Empty;
        }

        damageValue = 0;
        intervalTime = intervalThreshold;
        transform.localScale = originalScale;
    }

    /// <summary>释放性能预算并归还对象池，或回退到本地停用逻辑。</summary>
    private void DisableNumberComponent()
    {
        if (ServiceLocator.TryGet(out PerformanceManager performance))
        {
            performance.Release(PerformanceBudgetCategory.DamageNumber);
        }

        if (ServiceLocator.TryGet(out PoolManager poolManager) && poolManager.IsManagedInstance(gameObject))
        {
            poolManager.Despawn(gameObject);
            return;
        }

        OnDespawn();
        gameObject.SetActive(false);
        resetNumberText?.Invoke(gameObject);
    }
}
