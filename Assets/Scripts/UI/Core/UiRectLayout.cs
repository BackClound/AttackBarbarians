using UnityEngine;

/// <summary>
/// 按预设将 RectTransform 对齐到父节点边缘或拉伸，配合 padding 表达「距边多少逻辑像素」。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 TopBar / BottomNav / LeftRail 等分区根节点上。</para>
/// <para><b>获取方式：</b>搭建 UI 时选 Preset 与 Padding，Play 前可在 Inspector 点 Apply（EditMode 下通过 OnValidate 预览）。</para>
/// </remarks>
[DisallowMultipleComponent]
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class UiRectLayout : MonoBehaviour
{
    public enum LayoutPreset
    {
        /// <summary>全屏拉伸。</summary>

        FullStretch,
        /// <summary>顶部横条拉伸。</summary>

        TopStretch,
        /// <summary>底部横条拉伸。</summary>

        BottomStretch,
        /// <summary>左侧竖条拉伸。</summary>

        LeftStretch,
        /// <summary>右侧竖条拉伸。</summary>

        RightStretch,
        /// <summary>左上角锚定。</summary>

        TopLeft,
        /// <summary>右上角锚定。</summary>

        TopRight,
        /// <summary>左下角锚定。</summary>

        BottomLeft,
        /// <summary>右下角锚定。</summary>

        BottomRight,
        /// <summary>居中锚定。</summary>

        Center,
    }

    [SerializeField] private LayoutPreset preset = LayoutPreset.FullStretch;
    [SerializeField] private RectOffset padding = new RectOffset();
    [SerializeField] private Vector2 fixedSize = new Vector2(200f, 120f);
    [SerializeField] private bool applyOnAwake = true;

    private RectTransform _rect;

    /// <summary>按配置在 Awake 时应用布局预设。</summary>
    private void Awake()
    {
        if (applyOnAwake)
        {
            ApplyLayout();
        }
    }

    /// <summary>Inspector 修改时在编辑模式下预览布局。</summary>
    private void OnValidate()
    {
        ApplyLayout();
    }

    /// <summary>按当前 Preset 与 Padding 写回 RectTransform。</summary>
    public void ApplyLayout()
    {
        if (_rect == null)
        {
            _rect = GetComponent<RectTransform>();
        }

        switch (preset)
        {
            case LayoutPreset.FullStretch:
                StretchFull();
                break;
            case LayoutPreset.TopStretch:
                StretchTop();
                break;
            case LayoutPreset.BottomStretch:
                StretchBottom();
                break;
            case LayoutPreset.LeftStretch:
                StretchLeft();
                break;
            case LayoutPreset.RightStretch:
                StretchRight();
                break;
            case LayoutPreset.TopLeft:
                PinCorner(0f, 1f);
                break;
            case LayoutPreset.TopRight:
                PinCorner(1f, 1f);
                break;
            case LayoutPreset.BottomLeft:
                PinCorner(0f, 0f);
                break;
            case LayoutPreset.BottomRight:
                PinCorner(1f, 0f);
                break;
            case LayoutPreset.Center:
                PinCenter();
                break;
        }
    }

    /// <summary>应用全屏拉伸布局。</summary>
    private void StretchFull()
    {
        _rect.anchorMin = Vector2.zero;
        _rect.anchorMax = Vector2.one;
        _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.offsetMin = new Vector2(padding.left, padding.bottom);
        _rect.offsetMax = new Vector2(-padding.right, -padding.top);
    }

    /// <summary>应用顶部横条拉伸布局。</summary>
    private void StretchTop()
    {
        float height = Mathf.Max(0f, fixedSize.y);
        _rect.anchorMin = new Vector2(0f, 1f);
        _rect.anchorMax = new Vector2(1f, 1f);
        _rect.pivot = new Vector2(0.5f, 1f);
        _rect.offsetMin = new Vector2(padding.left, -(padding.top + height));
        _rect.offsetMax = new Vector2(-padding.right, -padding.top);
    }

    /// <summary>应用底部横条拉伸布局。</summary>
    private void StretchBottom()
    {
        float height = Mathf.Max(0f, fixedSize.y);
        _rect.anchorMin = new Vector2(0f, 0f);
        _rect.anchorMax = new Vector2(1f, 0f);
        _rect.pivot = new Vector2(0.5f, 0f);
        _rect.offsetMin = new Vector2(padding.left, padding.bottom);
        _rect.offsetMax = new Vector2(-padding.right, padding.bottom + height);
    }

    /// <summary>应用左侧竖条拉伸布局。</summary>
    private void StretchLeft()
    {
        float width = Mathf.Max(0f, fixedSize.x);
        _rect.anchorMin = new Vector2(0f, 0f);
        _rect.anchorMax = new Vector2(0f, 1f);
        _rect.pivot = new Vector2(0f, 0.5f);
        _rect.offsetMin = new Vector2(padding.left, padding.bottom);
        _rect.offsetMax = new Vector2(padding.left + width, -padding.top);
    }

    /// <summary>应用右侧竖条拉伸布局。</summary>
    private void StretchRight()
    {
        float width = Mathf.Max(0f, fixedSize.x);
        _rect.anchorMin = new Vector2(1f, 0f);
        _rect.anchorMax = new Vector2(1f, 1f);
        _rect.pivot = new Vector2(1f, 0.5f);
        _rect.offsetMin = new Vector2(-(padding.right + width), padding.bottom);
        _rect.offsetMax = new Vector2(-padding.right, -padding.top);
    }

    /// <summary>将矩形锚定到指定角落。</summary>
    private void PinCorner(float anchorX, float anchorY)
    {
        _rect.anchorMin = _rect.anchorMax = new Vector2(anchorX, anchorY);
        _rect.pivot = new Vector2(anchorX, anchorY);
        float x = anchorX < 0.5f ? padding.left : -padding.right;
        float y = anchorY > 0.5f ? -padding.top : padding.bottom;
        _rect.sizeDelta = fixedSize;
        _rect.anchoredPosition = new Vector2(x, y);
    }

    /// <summary>将矩形居中并固定尺寸。</summary>
    private void PinCenter()
    {
        _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
        _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.sizeDelta = fixedSize;
        _rect.anchoredPosition = Vector2.zero;
    }
}
