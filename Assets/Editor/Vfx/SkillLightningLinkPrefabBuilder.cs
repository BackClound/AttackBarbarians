#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 生成 Shader 链状闪电 Prefab（<see cref="SkillLightningLinkVfx"/>）。
/// </summary>
public static class SkillLightningLinkPrefabBuilder
{
    public const string PrefabPath = "Assets/Resources/VFX/Skill/SkillLightningLink.prefab";
    public const string MaterialPath = "Assets/Resources/VFX/Skill/M_SkillLightningLink.mat";
    public const string MeshPath = "Assets/Resources/VFX/Skill/SkillLightningBeam.asset";
    public const string ShaderPath = "Assets/Shaders/Skill/SH_SkillLightningLink_URP.shader";

    /// <summary>编辑器加载时延迟检查并补全缺失 Prefab。</summary>
    [InitializeOnLoadMethod]
    private static void EnsurePrefabOnLoad()
    {
        EditorApplication.delayCall += TryBuildIfMissing;
    }
    /// <summary>菜单：强制生成 SkillLightningLink Prefab 及依赖资产。</summary>
    [MenuItem("Attack Barbarians/VFX/Create Skill Lightning Link Prefab")]
    public static void CreateFromMenu()
    {
        CreateOrUpdatePrefab(force: true);
    }

    /// <summary>若 Prefab/材质/Mesh 缺失则自动生成。</summary>
    private static void TryBuildIfMissing()
    {
        if (!File.Exists(PrefabPath) || !File.Exists(MaterialPath) || !File.Exists(MeshPath))
        {
            CreateOrUpdatePrefab(force: true);
        }
    }

    /// <summary>创建或更新 SkillLightningLink Prefab。</summary>
    /// <param name="force">是否强制重建。</param>
    private static void CreateOrUpdatePrefab(bool force)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? string.Empty);

        if (!force && File.Exists(PrefabPath) && File.Exists(MaterialPath) && File.Exists(MeshPath))
        {
            return;
        }

        Material material = CreateOrLoadMaterial();
        if (material == null)
        {
            Debug.LogError($"SkillLightningLinkPrefabBuilder: 无法创建材质，请确认 Shader 存在：{ShaderPath}");
            return;
        }

        Mesh beamMesh = CreateOrLoadBeamMesh(1f, SkillLightningLinkVfx.DefaultReferenceLength);
        if (beamMesh == null)
        {
            Debug.LogError("SkillLightningLinkPrefabBuilder: 无法创建光束 Mesh 资产。");
            return;
        }

        GameObject root = new GameObject("SkillLightningLink");
        MeshFilter meshFilter = root.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = root.AddComponent<MeshRenderer>();
        root.AddComponent<SkillLightningLinkVfx>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.sortingLayerName = "Bullet";
        meshRenderer.sortingOrder = 50;
        meshFilter.sharedMesh = beamMesh;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        if (prefab != null)
        {
            Debug.Log($"SkillLightningLink Prefab 已生成：{PrefabPath}", prefab);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>创建或加载链状闪电材质。</summary>
    private static Material CreateOrLoadMaterial()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null)
        {
            shader = Shader.Find("AttackBarbarians/SkillLightningLink");
        }

        if (shader == null)
        {
            return null;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "M_SkillLightningLink" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        ApplyMaterialDefaults(material);
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>写入链状闪电材质默认 Shader 参数。</summary>
    /// <param name="material">目标材质。</param>
    private static void ApplyMaterialDefaults(Material material)
    {
        material.SetColor("_Color", new Color(0.2f, 0.4f, 1f, 0.95f));
        material.SetColor("_CoreColor", new Color(0.9f, 0.7f, 1f, 1f));
        material.SetFloat("_Intensity", 5f);
        material.SetFloat("_Thickness", 0.08f);
        material.SetFloat("_Jitter", 0.42f);
        material.SetFloat("_FbmScale", 9f);
        material.SetFloat("_FbmSpeed", 1.6f);
        material.SetFloat("_BoltCount", 3f);
        material.SetFloat("_FlickerSpeed", 16f);
        material.renderQueue = 3000;
    }

    /// <summary>加载或创建光束 Mesh 资产。</summary>
    /// <param name="width">光束宽度。</param>
    /// <param name="length">光束长度。</param>
    private static Mesh CreateOrLoadBeamMesh(float width, float length)
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (existing != null)
        {
            return existing;
        }

        Mesh mesh = CreateBeamMesh(width, length);
        AssetDatabase.CreateAsset(mesh, MeshPath);
        return mesh;
    }

    /// <summary>程序化生成四顶点光束 Mesh。</summary>
    /// <param name="width">光束宽度。</param>
    /// <param name="length">光束长度。</param>
    private static Mesh CreateBeamMesh(float width, float length)
    {
        float halfW = width * 0.5f;
        var mesh = new Mesh { name = "SkillLightningBeam" };
        mesh.vertices = new[]
        {
            new Vector3(-halfW, 0f, 0f),
            new Vector3(halfW, 0f, 0f),
            new Vector3(-halfW, length, 0f),
            new Vector3(halfW, length, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        mesh.RecalculateBounds();
        return mesh;
    }
}
#endif
