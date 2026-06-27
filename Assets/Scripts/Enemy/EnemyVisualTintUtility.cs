using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// 敌人外观染色能力检测与应用（Sprite / Spine / Mesh）。
/// </summary>
public static class EnemyVisualTintUtility
{
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

    /// <summary>外观染色能力。</summary>
    public enum TintCapability
    {
        None,
        SpriteRenderer,
        SpineSkeleton,
        MeshMaterialColor,
        Mixed
    }

    /// <summary>检测结果。</summary>
    public struct TintAuditResult
    {
        public string ConfigId;
        public string PrefabName;
        public TintCapability Capability;
        public int SpriteRendererCount;
        public int SpineSkeletonCount;
        public int MeshRendererCount;
        public bool SupportsDifficultyTint;
    }

    /// <summary>检测 Prefab 是否支持染色。</summary>
    public static TintAuditResult AuditPrefab(string configId, GameObject prefab)
    {
        TintAuditResult result = new TintAuditResult
        {
            ConfigId = configId,
            PrefabName = prefab != null ? prefab.name : "(null)",
            Capability = TintCapability.None
        };

        if (prefab == null)
        {
            return result;
        }

        result.SpriteRendererCount = prefab.GetComponentsInChildren<SpriteRenderer>(true).Length;
        result.SpineSkeletonCount = prefab.GetComponentsInChildren<SkeletonAnimation>(true).Length +
                                    prefab.GetComponentsInChildren<SkeletonGraphic>(true).Length;
        result.MeshRendererCount = prefab.GetComponentsInChildren<MeshRenderer>(true).Length;

        bool hasSprite = result.SpriteRendererCount > 0;
        bool hasSpine = result.SpineSkeletonCount > 0;
        bool hasMesh = result.MeshRendererCount > 0;
        int kindCount = (hasSprite ? 1 : 0) + (hasSpine ? 1 : 0) + (hasMesh ? 1 : 0);

        if (kindCount > 1)
        {
            result.Capability = TintCapability.Mixed;
        }
        else if (hasSprite)
        {
            result.Capability = TintCapability.SpriteRenderer;
        }
        else if (hasSpine)
        {
            result.Capability = TintCapability.SpineSkeleton;
        }
        else if (hasMesh)
        {
            result.Capability = TintCapability.MeshMaterialColor;
        }

        result.SupportsDifficultyTint = result.Capability != TintCapability.None;
        return result;
    }

    /// <summary>按难度档应用外观色调。</summary>
    public static void ApplyDifficultyTint(GameObject root, int tierIndex, int maxTierIndex)
    {
        if (root == null)
        {
            return;
        }

        EnemyVisualTintState state = EnsureState(root);
        state.Restore();
        Color tint = ResolveTierTintColor(tierIndex, maxTierIndex);

        SpriteRenderer[] sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < sprites.Length; i++)
        {
            SpriteRenderer sr = sprites[i];
            if (sr == null)
            {
                continue;
            }

            state.AddSprite(sr, sr.color);
            sr.color = sr.color * tint;
        }

        SkeletonAnimation[] skeletons = root.GetComponentsInChildren<SkeletonAnimation>(true);
        for (int i = 0; i < skeletons.Length; i++)
        {
            SkeletonAnimation skeleton = skeletons[i];
            if (skeleton == null || !skeleton.valid)
            {
                continue;
            }

            Color original = skeleton.skeleton.GetColor();
            state.AddSpine(skeleton, original);
            skeleton.skeleton.SetColor(original * tint);
        }

        MeshRenderer[] meshes = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < meshes.Length; i++)
        {
            MeshRenderer mesh = meshes[i];
            if (mesh == null)
            {
                continue;
            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            mesh.GetPropertyBlock(block);
            Color original = block.HasProperty(ColorPropertyId)
                ? block.GetColor(ColorPropertyId)
                : Color.white;
            state.AddMesh(mesh, original);
            block.SetColor(ColorPropertyId, original * tint);
            mesh.SetPropertyBlock(block);
        }
    }

    /// <summary>重置外观染色。</summary>
    public static void ResetTint(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        if (root.TryGetComponent(out EnemyVisualTintState state))
        {
            state.Restore();
        }
    }

    /// <summary>确保根节点存在染色状态组件。</summary>
    private static EnemyVisualTintState EnsureState(GameObject root)
    {
        if (!root.TryGetComponent(out EnemyVisualTintState state))
        {
            state = root.AddComponent<EnemyVisualTintState>();
        }

        return state;
    }

    /// <summary>按档位计算色调。</summary>
    private static Color ResolveTierTintColor(int tierIndex, int maxTierIndex)
    {
        tierIndex = Mathf.Max(0, tierIndex);
        maxTierIndex = Mathf.Max(1, maxTierIndex);
        float t = Mathf.Clamp01(tierIndex / (float)maxTierIndex);
        Color low = new Color(1f, 1f, 1f, 1f);
        Color high = new Color(1.15f, 0.72f, 0.72f, 1f);
        return Color.Lerp(low, high, t);
    }
}

/// <summary>
/// 挂在敌人根节点，缓存原始颜色以便对象池复用时还原。
/// </summary>
public sealed class EnemyVisualTintState : MonoBehaviour
{
    private readonly List<SpriteEntry> sprites = new List<SpriteEntry>(8);
    private readonly List<SpineEntry> spines = new List<SpineEntry>(4);
    private readonly List<MeshEntry> meshes = new List<MeshEntry>(4);
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

    /// <summary>记录 Sprite 原始色。</summary>
    public void AddSprite(SpriteRenderer renderer, Color original)
    {
        sprites.Add(new SpriteEntry { Renderer = renderer, Original = original });
    }

    /// <summary>记录 Spine 原始色。</summary>
    public void AddSpine(SkeletonAnimation skeleton, Color original)
    {
        spines.Add(new SpineEntry { Skeleton = skeleton, Original = original });
    }

    /// <summary>记录 Mesh 原始色。</summary>
    public void AddMesh(MeshRenderer renderer, Color original)
    {
        meshes.Add(new MeshEntry { Renderer = renderer, Original = original });
    }

    /// <summary>还原全部染色。</summary>
    public void Restore()
    {
        for (int i = 0; i < sprites.Count; i++)
        {
            SpriteEntry entry = sprites[i];
            if (entry.Renderer != null)
            {
                entry.Renderer.color = entry.Original;
            }
        }

        for (int i = 0; i < spines.Count; i++)
        {
            SpineEntry entry = spines[i];
            if (entry.Skeleton != null && entry.Skeleton.valid)
            {
                entry.Skeleton.skeleton.SetColor(entry.Original);
            }
        }

        for (int i = 0; i < meshes.Count; i++)
        {
            MeshEntry entry = meshes[i];
            if (entry.Renderer == null)
            {
                continue;
            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            entry.Renderer.GetPropertyBlock(block);
            block.SetColor(ColorPropertyId, entry.Original);
            entry.Renderer.SetPropertyBlock(block);
        }

        sprites.Clear();
        spines.Clear();
        meshes.Clear();
    }

    private struct SpriteEntry
    {
        public SpriteRenderer Renderer;
        public Color Original;
    }

    private struct SpineEntry
    {
        public SkeletonAnimation Skeleton;
        public Color Original;
    }

    private struct MeshEntry
    {
        public MeshRenderer Renderer;
        public Color Original;
    }
}
