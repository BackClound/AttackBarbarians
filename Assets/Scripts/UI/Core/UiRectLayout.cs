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
        FullStretch,
        TopStretch,
        BottomStretch,
        LeftStretch,
        RightStretch,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center,
    }

    [SerializeField] private LayoutPreset preset = LayoutPreset.FullStretch;
    [SerializeField] private RectOffset padding = new RectOffset();
    [SerializeField] private Vector2 fixedSize = new Vector2(200f, 120f);
    [SerializeField] private bool applyOnAwake = true;

    private RectTransform _rect;

    private void Awake()
    {
        if (applyOnAwake)
        {
            ApplyLayout();
        }
    }

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

    private void StretchFull()
    {
        _rect.anchorMin = Vector2.zero;
        _rect.anchorMax = Vector2.one;
        _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.offsetMin = new Vector2(padding.left, padding.bottom);
        _rect.offsetMax = new Vector2(-padding.right, -padding.top);
    }

    private void StretchTop()
    {
        float height = Mathf.Max(0f, fixedSize.y);
        _rect.anchorMin = new Vector2(0f, 1f);
        _rect.anchorMax = new Vector2(1f, 1f);
        _rect.pivot = new Vector2(0.5f, 1f);
        _rect.offsetMin = new Vector2(padding.left, -(padding.top + height));
        _rect.offsetMax = new Vector2(-padding.right, -padding.top);
    }

    private void StretchBottom()
    {
        float height = Mathf.Max(0f, fixedSize.y);
        _rect.anchorMin = new Vector2(0f, 0f);
        _rect.anchorMax = new Vector2(1f, 0f);
        _rect.pivot = new Vector2(0.5f, 0f);
        _rect.offsetMin = new Vector2(padding.left, padding.bottom);
        _rect.offsetMax = new Vector2(-padding.right, padding.bottom + height);
    }

    private void StretchLeft()
    {
        float width = Mathf.Max(0f, fixedSize.x);
        _rect.anchorMin = new Vector2(0f, 0f);
        _rect.anchorMax = new Vector2(0f, 1f);
        _rect.pivot = new Vector2(0f, 0.5f);
        _rect.offsetMin = new Vector2(padding.left, padding.bottom);
        _rect.offsetMax = new Vector2(padding.left + width, -padding.top);
    }

    private void StretchRight()
    {
        float width = Mathf.Max(0f, fixedSize.x);
        _rect.anchorMin = new Vector2(1f, 0f);
        _rect.anchorMax = new Vector2(1f, 1f);
        _rect.pivot = new Vector2(1f, 0.5f);
        _rect.offsetMin = new Vector2(-(padding.right + width), padding.bottom);
        _rect.offsetMax = new Vector2(-padding.right, -padding.top);
    }

    private void PinCorner(float anchorX, float anchorY)
    {
        _rect.anchorMin = _rect.anchorMax = new Vector2(anchorX, anchorY);
        _rect.pivot = new Vector2(anchorX, anchorY);
        float x = anchorX < 0.5f ? padding.left : -padding.right;
        float y = anchorY > 0.5f ? -padding.top : padding.bottom;
        _rect.sizeDelta = fixedSize;
        _rect.anchoredPosition = new Vector2(x, y);
    }

    private void PinCenter()
    {
        _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
        _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.sizeDelta = fixedSize;
        _rect.anchoredPosition = Vector2.zero;
    }
}
