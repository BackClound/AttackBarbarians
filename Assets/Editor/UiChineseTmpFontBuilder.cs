using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// 生成 Noto Sans SC 的 TMP SDF 资源，并修复 MainScene 中文显示。
/// </summary>
public static class UiChineseTmpFontBuilder
{
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";

    [MenuItem("Attack Barbarians/UI/Setup Chinese TMP Font")]
    public static void SetupChineseTmpFont()
    {
        TMP_FontAsset chineseFont = CreateOrLoadChineseFontAsset();
        if (chineseFont == null)
        {
            return;
        }

        RegisterGlobalFallback(chineseFont);
        RegisterLiberationFallback(chineseFont);
        AssetDatabase.SaveAssets();
        Debug.Log($"[UiChineseTmpFontBuilder] 中文字体已就绪：{UiTmpFontPaths.SdfFontAssetPath}");
    }

    [MenuItem("Attack Barbarians/UI/Apply Chinese Font To MainScene")]
    public static void ApplyChineseFontToMainScene()
    {
        TMP_FontAsset chineseFont = CreateOrLoadChineseFontAsset();
        if (chineseFont == null)
        {
            return;
        }

        RegisterGlobalFallback(chineseFont);
        RegisterLiberationFallback(chineseFont);

        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        int count = ApplyToScene(chineseFont);
        EnsureBootstrapOnMainSceneUi();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"[UiChineseTmpFontBuilder] MainScene 已更新 {count} 个 TMP 文本，并挂载 UiTmpChineseFontBootstrap。");
    }

    public static TMP_FontAsset CreateOrLoadChineseFontAsset()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiTmpFontPaths.SdfFontAssetPath);
        if (UiTmpChineseFont.IsUsable(existing))
        {
            return existing;
        }

        if (existing != null)
        {
            Debug.LogWarning(
                "[UiChineseTmpFontBuilder] 检测到损坏的 NotoSansSC SDF（缺少 Atlas/Material），正在重建…");
            AssetDatabase.DeleteAsset(UiTmpFontPaths.SdfFontAssetPath);
        }

        if (!File.Exists(UiTmpFontPaths.SourceFontAssetPath))
        {
            Debug.LogError(
                $"[UiChineseTmpFontBuilder] 缺少源字体文件：{UiTmpFontPaths.SourceFontAssetPath}");
            return null;
        }

        Font source = AssetDatabase.LoadAssetAtPath<Font>(UiTmpFontPaths.SourceFontAssetPath);
        if (source == null)
        {
            Debug.LogError(
                "[UiChineseTmpFontBuilder] 无法加载字体，请在 Inspector 中确认 NotoSansSC-Regular 已导入且 Include Font Data 已勾选。");
            return null;
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            source,
            36,
            5,
            GlyphRenderMode.SDFAA,
            2048,
            2048,
            AtlasPopulationMode.Dynamic);

        if (fontAsset == null)
        {
            Debug.LogError("[UiChineseTmpFontBuilder] CreateFontAsset 失败。");
            return null;
        }

        fontAsset.name = "NotoSansSC SDF";
        SaveFontAssetWithSubAssets(fontAsset, UiTmpFontPaths.SdfFontAssetPath);

        TMP_FontAsset saved = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiTmpFontPaths.SdfFontAssetPath);
        if (!UiTmpChineseFont.IsUsable(saved))
        {
            Debug.LogError("[UiChineseTmpFontBuilder] 字体资源保存后仍无效，请检查 TMP / TextCore 模块。");
            return null;
        }

        return saved;
    }

    /// <summary>
    /// 将 Atlas 纹理与 Material 作为子资源写入同一 .asset，避免 m_AtlasTextures 丢失。
    /// </summary>
    private static void SaveFontAssetWithSubAssets(TMP_FontAsset fontAsset, string assetPath)
    {
        AssetDatabase.CreateAsset(fontAsset, assetPath);

        Texture2D[] textures = fontAsset.atlasTextures;
        if (textures != null)
        {
            for (int i = 0; i < textures.Length; i++)
            {
                Texture2D texture = textures[i];
                if (texture == null)
                {
                    continue;
                }

                texture.name = $"{fontAsset.name} Atlas {i}";
                AssetDatabase.AddObjectToAsset(texture, fontAsset);
            }
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = $"{fontAsset.name} Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void RegisterGlobalFallback(TMP_FontAsset chineseFont)
    {
        TMP_Settings settings = Resources.Load<TMP_Settings>("TMP Settings");
        if (settings == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(settings);
        SerializedProperty fallbackList = serialized.FindProperty("m_fallbackFontAssets");
        if (fallbackList == null)
        {
            return;
        }

        if (!ContainsFontReference(fallbackList, chineseFont))
        {
            fallbackList.InsertArrayElementAtIndex(fallbackList.arraySize);
            fallbackList.GetArrayElementAtIndex(fallbackList.arraySize - 1).objectReferenceValue = chineseFont;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }
    }

    private static void RegisterLiberationFallback(TMP_FontAsset chineseFont)
    {
        TMP_FontAsset liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (liberation == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(liberation);
        SerializedProperty fallbackTable = serialized.FindProperty("m_FallbackFontAssetTable");
        if (fallbackTable == null)
        {
            return;
        }

        if (!ContainsFontReference(fallbackTable, chineseFont))
        {
            fallbackTable.InsertArrayElementAtIndex(0);
            fallbackTable.GetArrayElementAtIndex(0).objectReferenceValue = chineseFont;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(liberation);
        }
    }

    private static bool ContainsFontReference(SerializedProperty list, TMP_FontAsset font)
    {
        for (int i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == font)
            {
                return true;
            }
        }

        return false;
    }

    private static int ApplyToScene(TMP_FontAsset chineseFont)
    {
        TMP_Text[] texts = Object.FindObjectsOfType<TMP_Text>(true);
        int count = 0;
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text.font == chineseFont)
            {
                continue;
            }

            Undo.RecordObject(text, "Apply Chinese TMP Font");
            text.font = chineseFont;
            text.ForceMeshUpdate();
            EditorUtility.SetDirty(text);
            count++;
        }

        return count;
    }

    private static void EnsureBootstrapOnMainSceneUi()
    {
        GameObject canvas = GameObject.Find("MainSceneUI");
        if (canvas == null)
        {
            return;
        }

        if (canvas.GetComponent<UiTmpChineseFontBootstrap>() == null)
        {
            Undo.AddComponent<UiTmpChineseFontBootstrap>(canvas);
        }
    }
}
