using UnityEngine;

/// <summary>
/// 轻量技能施法特效播放器（链状闪电等）；无 Prefab 时使用 <see cref="LineRenderer"/> 一次性线段。
/// Buff tier 越高，闪电颜色越深偏紫。
/// </summary>
/// <remarks><b>是否需要挂载：</b>否。静态调用。</remarks>
public static class SkillCastVfxPlayer
{
    private const float DefaultLineDuration = 0.12f;
    private const int MaxActiveLines = 24;

    private static readonly SkillLightningLine[] LinePool = new SkillLightningLine[MaxActiveLines];
    private static int linePoolCount;

    /// <summary>
    /// 绘制闪电链线段；<paramref name="buffTier"/> 越高颜色越偏紫。
    /// </summary>
    /// <param name="from">线段起点世界坐标。</param>
    /// <param name="to">线段终点世界坐标。</param>
    /// <param name="buffTier">Buff 层级，用于颜色插值。</param>
    public static void PlayLightningSegment(Vector2 from, Vector2 to, int buffTier)
    {
        Color color = ResolveLightningColor(buffTier);
        SkillLightningLine line = RentLine();
        line.Play(from, to, color, DefaultLineDuration);
    }

    /// <summary>按 Buff tier 在蓝白与紫色之间插值闪电颜色。</summary>
    /// <param name="buffTier">Buff 层级。</param>
    /// <returns>闪电线段颜色。</returns>
    private static Color ResolveLightningColor(int buffTier)
    {
        int tier = Mathf.Max(1, buffTier);
        float t = Mathf.Clamp01((tier - 1) / 4f);
        return Color.Lerp(new Color(0.55f, 0.85f, 1f, 0.95f), new Color(0.65f, 0.2f, 0.95f, 1f), t);
    }

    /// <summary>从内部池租借或创建闪电线段渲染器。</summary>
    /// <returns>可用的线段实例。</returns>
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

    /// <summary>闪电线段渲染器：短时显示后自动隐藏。</summary>
    private sealed class SkillLightningLine : MonoBehaviour
    {
        private LineRenderer lineRenderer;
        private float remaining;

        /// <summary>线段是否仍在显示中。</summary>
        public bool IsActive => remaining > 0f;

        /// <summary>
        /// 在两点间绘制彩色线段并启动倒计时。
        /// </summary>
        /// <param name="from">起点。</param>
        /// <param name="to">终点。</param>
        /// <param name="color">线段颜色。</param>
        /// <param name="duration">显示时长（秒）。</param>
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

        /// <summary>每帧递减剩余显示时长，到期后隐藏线段。</summary>
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

        /// <summary>懒初始化 LineRenderer 组件与默认材质。</summary>
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
