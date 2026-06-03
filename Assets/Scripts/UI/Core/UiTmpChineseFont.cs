using TMPro;
using UnityEngine;

/// <summary>
/// 为 TextMeshPro 提供可显示简体中文的字体（Noto Sans SC，动态图集）。
/// </summary>
public static class UiTmpChineseFont
{
    private static TMP_FontAsset s_cachedFont;

    public static bool IsUsable(TMP_FontAsset font)
    {
        if (font == null || font.material == null)
        {
            return false;
        }

        Texture2D[] textures = font.atlasTextures;
        return textures != null && textures.Length > 0 && textures[0] != null;
    }

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
