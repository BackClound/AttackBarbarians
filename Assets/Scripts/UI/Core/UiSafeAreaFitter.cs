using UnityEngine;

/// <summary>
/// 将 <see cref="Screen.safeArea"/> 应用到当前 RectTransform，作为 Canvas 下安全区根节点。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 SafeAreaRoot（Canvas 子物体）上。</para>
/// <para><b>获取方式：</b>MainScene / 各页面 UI 根下建空节点 SafeAreaRoot，所有可交互 UI 作为其子物体。</para>
/// </remarks>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UiSafeAreaFitter : MonoBehaviour
{
    [SerializeField] private bool applyOnAwake = true;
    [SerializeField] private bool listenForScreenChange = true;

    private RectTransform _rect;
    private Rect _lastSafeArea;
    private Vector2Int _lastScreenSize;

    /// <summary>缓存 RectTransform 并按需应用安全区。</summary>
    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (applyOnAwake)
        {
            ApplySafeArea();
        }
    }

    /// <summary>启用时重新应用安全区。</summary>
    private void OnEnable()
    {
        ApplySafeArea();
    }

    /// <summary>监听屏幕尺寸或安全区变化并自动刷新布局。</summary>
    private void Update()
    {
        if (!listenForScreenChange)
        {
            return;
        }

        if (_lastSafeArea != Screen.safeArea || _lastScreenSize.x != Screen.width || _lastScreenSize.y != Screen.height)
        {
            ApplySafeArea();
        }
    }

    /// <summary>立即按当前屏幕安全区刷新锚点与偏移。</summary>
    public void ApplySafeArea()
    {
        if (_rect == null)
        {
            _rect = GetComponent<RectTransform>();
        }

        Rect safe = Screen.safeArea;
        _lastSafeArea = safe;
        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        _rect.anchorMin = min;
        _rect.anchorMax = max;
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
    }
}
