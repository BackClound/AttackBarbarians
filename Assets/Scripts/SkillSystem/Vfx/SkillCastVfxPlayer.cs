using UnityEngine;

/// <summary>
/// 轻量技能施法特效（链状闪电等）；无 Prefab 时使用 <see cref="LineRenderer"/> 一次性线段。
/// </summary>
/// <remarks><b>是否需要挂载：</b>否。静态调用。</remarks>
public static class SkillCastVfxPlayer
{
    private const float DefaultLineDuration = 0.12f;
    private const int MaxActiveLines = 24;

    private static readonly SkillLightningLine[] LinePool = new SkillLightningLine[MaxActiveLines];
    private static int linePoolCount;

    /// <summary>
    /// 绘制闪电链；<paramref name="buffTier"/> 越高颜色越偏紫。
    /// </summary>
    public static void PlayLightningSegment(Vector2 from, Vector2 to, int buffTier)
    {
        Color color = ResolveLightningColor(buffTier);
        SkillLightningLine line = RentLine();
        line.Play(from, to, color, DefaultLineDuration);
    }

    private static Color ResolveLightningColor(int buffTier)
    {
        int tier = Mathf.Max(1, buffTier);
        float t = Mathf.Clamp01((tier - 1) / 4f);
        return Color.Lerp(new Color(0.55f, 0.85f, 1f, 0.95f), new Color(0.65f, 0.2f, 0.95f, 1f), t);
    }

    private static SkillLightningLine RentLine()
    {
        for (int i = 0; i < linePoolCount; i++)
        {
            if (LinePool[i] != null && !LinePool[i].IsActive)
            {
                return LinePool[i];
            }
        }

        if (linePoolCount < MaxActiveLines)
        {
            var host = new GameObject("SkillLightningVfx");
            Object.DontDestroyOnLoad(host);
            var line = host.AddComponent<SkillLightningLine>();
            LinePool[linePoolCount++] = line;
            return line;
        }

        return LinePool[0];
    }

    private sealed class SkillLightningLine : MonoBehaviour
    {
        private LineRenderer lineRenderer;
        private float remaining;

        public bool IsActive => remaining > 0f;

        public void Play(Vector2 from, Vector2 to, Color color, float duration)
        {
            EnsureRenderer();
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, from);
            lineRenderer.SetPosition(1, to);
            lineRenderer.enabled = true;
            remaining = duration;
        }

        private void Update()
        {
            if (remaining <= 0f)
            {
                if (lineRenderer != null)
                {
                    lineRenderer.enabled = false;
                }

                return;
            }

            remaining -= Time.deltaTime;
            if (remaining <= 0f && lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }

        private void EnsureRenderer()
        {
            if (lineRenderer != null)
            {
                return;
            }

            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.widthMultiplier = 0.08f;
            lineRenderer.positionCount = 2;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.sortingOrder = 50;
        }
    }
}
