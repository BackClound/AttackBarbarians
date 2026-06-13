using TMPro;
using UnityEngine;

/// <summary>
/// 为 TextMeshPro 提供可显示简体中文的字体（Noto Sans SC，动态图集）。
/// </summary>
public static class UiTmpChineseFont
{
    private static TMP_FontAsset s_cachedFont;

    /// <summary>判断 TMP 字体资源是否可用（材质与图集有效）。</summary>
        /// <param name="font">待检测的字体资源。</param>
        /// <returns>可用返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public static bool IsUsable(TMP_FontAsset font)
    {
        if (font == null || font.material == null)
        {
            return false;
        }

        Texture2D[] textures = font.atlasTextures;
        return textures != null && textures.Length > 0 && textures[0] != null;
    }

    /// <summary>获取或创建简体中文 TMP 字体（优先磁盘 SDF，回退 Resources 动态生成）。</summary>
        /// <returns>可用的 <see cref="TMP_FontAsset"/>；失败时返回 <c>null</c>。</returns>
    public static TMP_FontAsset GetFont()
    {
        if (IsUsable(s_cachedFont))
        {
            return s_cachedFont;
        }

        s_cachedFont = null;

#if UNITY_EDITOR
        TMP_FontAsset fromDisk = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiTmpFontPaths.SdfFontAssetPath);
        if (IsUsable(fromDisk))
        {
            s_cachedFont = fromDisk;
            return s_cachedFont;
        }
#endif

        Font source = Resources.Load<Font>(UiTmpFontPaths.SourceFontResourcePath);
        if (source == null)
        {
            Debug.LogWarning(
                $"[UiTmpChineseFont] 未找到字体：Resources/{UiTmpFontPaths.SourceFontResourcePath}。请在 Tuanjie 菜单执行「Attack Barbarians/UI/Setup Chinese TMP Font」。");
            return null;
        }

        // 使用 TMP 默认 SDF 参数，避免直接引用 UnityEngine.TextCore.LowLevel.GlyphRenderMode
        s_cachedFont = TMP_FontAsset.CreateFontAsset(source);

        if (s_cachedFont != null)
        {
            s_cachedFont.name = "NotoSansSC SDF (Runtime)";
        }

        return s_cachedFont;
    }

    /// <summary>将中文字体应用到单个 TMP 文本组件。</summary>
        /// <param name="text">目标 TMP 文本；为 <c>null</c> 时忽略。</param>
    public static void Apply(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        TMP_FontAsset font = GetFont();
        if (!IsUsable(font))
        {
            return;
        }

        if (text.font == font)
        {
            return;
        }

        text.font = font;
        text.ForceMeshUpdate();
    }

    /// <summary>为子树内所有 TMP 文本应用中文字体。</summary>
        /// <param name="root">遍历根节点；为 <c>null</c> 时忽略。</param>
    public static void ApplyAllInChildren(Transform root)
    {
        if (root == null)
        {
            return;
        }

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            Apply(texts[i]);
        }
    }
}
