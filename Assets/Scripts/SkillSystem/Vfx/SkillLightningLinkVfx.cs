using UnityEngine;

/// <summary>
/// 链状闪电（Shader 四边形）：轴心在起点，沿本地 +Y 拉伸至终点。
/// 兼容 URP 2D Renderer（Shader 需含 Universal2D Pass）。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SkillLightningLinkVfx : MonoBehaviour
{
    public const float DefaultReferenceLength = 5f;

    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int StartTimeId = Shader.PropertyToID("_StartTime");
    private static readonly int FadeId = Shader.PropertyToID("_Fade");

    private static Mesh sharedBeamMesh;

    [SerializeField] private float referenceLength = DefaultReferenceLength;
    [SerializeField] private float thicknessScale = 0.65f;
    [SerializeField] private float beamWidth = 1f;
    [SerializeField] private float baseIntensity = 5f;
    [SerializeField] private string materialResourcePath = "VFX/Skill/M_SkillLightningLink";
    [SerializeField] private string meshResourcePath = "VFX/Skill/SkillLightningBeam";

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Material runtimeMaterial;
    private float playStartTime;
    private float playDuration;
    private bool isPlaying;

    public float ReferenceLength => referenceLength;
    public float ThicknessScale => thicknessScale;

    private void Awake()
    {
        EnsureComponents();
    }

    public void ApplyLink(Vector2 from, Vector2 to, float depth)
    {
        EnsureComponents();

        Vector3 start = new Vector3(from.x, from.y, depth);
        Vector3 end = new Vector3(to.x, to.y, depth);
        Vector3 delta = end - start;
        float distance = delta.magnitude;
        if (distance <= 0.01f)
        {
            meshRenderer.enabled = false;
            return;
        }

        Vector3 direction = delta / distance;
        transform.position = start;
        transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
        float lengthScale = distance / Mathf.Max(0.01f, referenceLength);
        transform.localScale = new Vector3(thicknessScale, lengthScale, 1f);
        meshRenderer.enabled = true;
    }

    public void PlayVisuals(float durationSeconds)
    {
        EnsureComponents();
        EnsureRuntimeMaterial();

        playDuration = Mathf.Max(0.01f, durationSeconds);
        playStartTime = Time.time;
        isPlaying = true;

        propertyBlock.SetFloat(StartTimeId, playStartTime);
        propertyBlock.SetFloat(FadeId, 1f);
        propertyBlock.SetFloat(IntensityId, baseIntensity);
        meshRenderer.SetPropertyBlock(propertyBlock);
        meshRenderer.enabled = true;
    }

    private void Update()
    {
        if (!isPlaying || meshRenderer == null)
        {
            return;
        }

        float elapsed = Time.time - playStartTime;
        if (elapsed >= playDuration)
        {
            isPlaying = false;
            meshRenderer.enabled = false;
            return;
        }

        float fadeOut = 1f;
        const float fadeWindow = 0.25f;
        if (elapsed > playDuration - fadeWindow)
        {
            fadeOut = Mathf.Clamp01((playDuration - elapsed) / fadeWindow);
        }

        propertyBlock.SetFloat(FadeId, fadeOut);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    private void Reset()
    {
        CacheComponentReferences();
    }

    private void CacheComponentReferences()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }
    }

    private void EnsureRequiredComponents()
    {
        CacheComponentReferences();

        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        propertyBlock ??= new MaterialPropertyBlock();
    }

    private void EnsureComponents()
    {
        EnsureRequiredComponents();
        if (meshFilter == null || meshRenderer == null)
        {
            return;
        }

        if (meshFilter.sharedMesh == null)
        {
            Mesh loadedMesh = Resources.Load<Mesh>(meshResourcePath);
            if (loadedMesh != null)
            {
                meshFilter.sharedMesh = loadedMesh;
            }
            else
            {
                sharedBeamMesh ??= CreateBeamMesh(beamWidth, referenceLength);
                meshFilter.sharedMesh = sharedBeamMesh;
            }
        }

        EnsureRuntimeMaterial();

        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        meshRenderer.sortingLayerName = "Bullet";
        meshRenderer.sortingOrder = 50;
    }

    private void EnsureRuntimeMaterial()
    {
        if (meshRenderer == null)
        {
            return;
        }

        Material loaded = Resources.Load<Material>(materialResourcePath);
        if (IsMaterialValid(loaded))
        {
            if (runtimeMaterial != loaded)
            {
                runtimeMaterial = loaded;
                meshRenderer.sharedMaterial = loaded;
            }

            return;
        }

        Shader shader = Shader.Find("AttackBarbarians/CopyLightning");
        if (shader == null)
        {
            shader = Shader.Find("AttackBarbarians/SkillLightningLink");
        }
        if (shader != null && shader.isSupported)
        {
            if (runtimeMaterial == null || runtimeMaterial.shader != shader)
            {
                runtimeMaterial = new Material(shader);
                meshRenderer.sharedMaterial = runtimeMaterial;
            }

            return;
        }

        Shader fallback = Shader.Find("Sprites/Default");
        if (fallback != null)
        {
            runtimeMaterial = new Material(fallback) { color = new Color(0.45f, 0.55f, 1f, 0.85f) };
            meshRenderer.sharedMaterial = runtimeMaterial;
        }
    }

    private static bool IsMaterialValid(Material material)
    {
        return material != null
            && material.shader != null
            && material.shader.isSupported
            && material.shader.name != "Hidden/InternalErrorShader";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        // OnValidate 可能在 RequireComponent 尚未挂上时触发，此处仅缓存引用，不 AddComponent。
        CacheComponentReferences();
        if (meshFilter == null || meshRenderer == null)
        {
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();
    }
#endif

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
        mesh.RecalculateNormals();
        return mesh;
    }
}
