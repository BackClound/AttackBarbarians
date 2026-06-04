#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MainScene 页面 UI 搭建公用工具：背景 + SafeArea + 底栏预留，对齐 <c>ui_mobile_adaptation_design.md</c>。
/// </summary>
public static class UiPageStructureEditorUtility
{
    /// <summary>公共 BottomNav 逻辑高度（与 UiRectLayout BottomStretch 一致）。</summary>
    public const float BottomNavHeight = 160f;

    public const float BottomNavHorizontalPadding = 12f;
    public const float BottomNavBottomPadding = 8f;

    /// <summary>SafeArea 内为底栏预留的底部边距（逻辑像素）。</summary>
    public static int SafeAreaBottomInset => Mathf.RoundToInt(BottomNavHeight + BottomNavBottomPadding);

    public static readonly Color DefaultPageBackground = Hex("#06112F");

    /// <summary>
    /// 创建页面壳：全屏拉伸根节点 + 背景图 + SafeAreaRoot（含 <see cref="UiSafeAreaFitter"/>）。
    /// </summary>
    public static PageShell CreatePageShell(
        RectTransform canvasRoot,
        string pageRootName,
        System.Type presenterType,
        Color backgroundColor)
    {
        GameObject pageGo = new GameObject(pageRootName, typeof(RectTransform), presenterType, typeof(UiRectLayout));
        pageGo.transform.SetParent(canvasRoot, false);
        StretchFull(pageGo.GetComponent<RectTransform>());
        SetLayout(pageGo.GetComponent<UiRectLayout>(), UiRectLayout.LayoutPreset.FullStretch, new RectOffset());

        Image background = CreateStretchBackground(pageGo.transform, "PageBackground", backgroundColor);

        GameObject safeGo = new GameObject("SafeAreaRoot", typeof(RectTransform), typeof(UiSafeAreaFitter));
        safeGo.transform.SetParent(pageGo.transform, false);
        RectTransform safeRect = safeGo.GetComponent<RectTransform>();
        StretchFull(safeRect);

        UiRectLayout safeLayout = safeGo.AddComponent<UiRectLayout>();
        SetLayout(
            safeLayout,
            UiRectLayout.LayoutPreset.FullStretch,
            new RectOffset(0, 0, 0, SafeAreaBottomInset));

        return new PageShell
        {
            PageRoot = pageGo.transform,
            SafeAreaRoot = safeRect,
            BackgroundImage = background,
        };
    }

    /// <summary>
    /// 将 BottomNav 固定在 Canvas 底部，置于同级最后以覆盖页面内容，且不放入 ScrollView。
    /// </summary>
    public static void PinBottomNavToCanvas(RectTransform canvasRoot, Transform bottomNav)
    {
        if (canvasRoot == null || bottomNav == null)
        {
            return;
        }

        bottomNav.SetParent(canvasRoot, false);
        bottomNav.SetAsLastSibling();

        UiRectLayout layout = bottomNav.GetComponent<UiRectLayout>();
        if (layout == null)
        {
            layout = bottomNav.gameObject.AddComponent<UiRectLayout>();
        }

        SetLayout(
            layout,
            UiRectLayout.LayoutPreset.BottomStretch,
            new RectOffset(
                (int)BottomNavHorizontalPadding,
                (int)BottomNavHorizontalPadding,
                0,
                (int)BottomNavBottomPadding),
            new Vector2(0f, BottomNavHeight));
    }

    public static Image CreateStretchBackground(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        StretchFull(go.GetComponent<RectTransform>());
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static void StretchFull(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    public static void SetLayout(
        UiRectLayout layout,
        UiRectLayout.LayoutPreset preset,
        RectOffset padding,
        Vector2 fixedSize = default)
    {
        if (layout == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(layout);
        so.FindProperty("preset").enumValueIndex = (int)preset;
        SetSerializedRectOffset(so.FindProperty("padding"), padding);
        if (fixedSize != default)
        {
            so.FindProperty("fixedSize").vector2Value = fixedSize;
        }

        so.FindProperty("applyOnAwake").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        layout.ApplyLayout();
    }

    public static void SetSerializedRectOffset(SerializedProperty paddingProp, RectOffset padding)
    {
        if (paddingProp == null || padding == null)
        {
            return;
        }

        paddingProp.FindPropertyRelative("m_Left").intValue = padding.left;
        paddingProp.FindPropertyRelative("m_Right").intValue = padding.right;
        paddingProp.FindPropertyRelative("m_Top").intValue = padding.top;
        paddingProp.FindPropertyRelative("m_Bottom").intValue = padding.bottom;
    }

    private static Color Hex(string html)
    {
        return ColorUtility.TryParseHtmlString(html, out Color color) ? color : Color.white;
    }

    public struct PageShell
    {
        public Transform PageRoot;
        public RectTransform SafeAreaRoot;
        public Image BackgroundImage;
    }
}
#endif
