using UnityEngine;

/// <summary>
/// 技能施法特效播放器：链状闪电使用 Vefects Zap Prefab，在两点间拉伸并播放粒子。
/// </summary>
/// <remarks><b>是否需要挂载：</b>否。静态调用。</remarks>
public static class SkillCastVfxPlayer
{
    private const float DefaultBoltDuration = 1.1f;
    private const float BoltReferenceLength = 5f;
    private const int MaxActiveBolts = 24;
    private const string VfxSortingLayer = "Bullet";
    private const int VfxSortingOrder = 50;
    private const float VfxDepth = -1f;

    private static readonly SkillLightningBolt[] BoltPool = new SkillLightningBolt[MaxActiveBolts];
    private static int boltPoolCount;

    /// <summary>
    /// 在两点间播放闪电链特效。
    /// </summary>
    /// <param name="from">线段起点世界坐标。</param>
    /// <param name="to">线段终点世界坐标。</param>
    /// <param name="prefab">闪电 Prefab（Vefects Zap 系列）。</param>
    /// <param name="buffTier">Buff 层级，预留用于后续颜色变体。</param>
    public static void PlayLightningSegment(Vector2 from, Vector2 to, GameObject prefab, int buffTier = 1)
    {
        if (prefab == null)
        {
            return;
        }

        SkillLightningBolt bolt = RentBolt(prefab);
        bolt.Play(from, to, DefaultBoltDuration);
    }

    /// <summary>从内部池租借或创建闪电实例。</summary>
    /// <param name="prefab">闪电 Prefab 模板。</param>
    /// <returns>可用的闪电实例。</returns>
    private static SkillLightningBolt RentBolt(GameObject prefab)
    {
        for (int i = 0; i < boltPoolCount; i++)
        {
            if (BoltPool[i] != null && !BoltPool[i].IsActive)
            {
                return BoltPool[i];
            }
        }

        if (boltPoolCount < MaxActiveBolts)
        {
            GameObject host = Object.Instantiate(prefab);
            host.name = "SkillLightningVfx";
            Object.DontDestroyOnLoad(host);
            ApplySorting(host);
            var bolt = host.AddComponent<SkillLightningBolt>();
            BoltPool[boltPoolCount++] = bolt;
            return bolt;
        }

        return BoltPool[0];
    }

    /// <summary>
    /// Vefects Prefab 默认在 Default 排序层，会被 Wall / Enemy / Player 层完全遮挡。
    /// </summary>
    /// <param name="host">闪电实例根节点。</param>
    private static void ApplySorting(GameObject host)
    {
        ParticleSystemRenderer[] renderers = host.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerName = VfxSortingLayer;
            renderers[i].sortingOrder = VfxSortingOrder;
        }
    }

    /// <summary>闪电 Prefab 实例控制器：定位、拉伸并定时回收显示。</summary>
    private sealed class SkillLightningBolt : MonoBehaviour
    {
        private float remaining;

        /// <summary>特效是否仍在播放中。</summary>
        public bool IsActive => remaining > 0f;

        /// <summary>
        /// 将闪电 Prefab 从起点拉伸至终点并播放粒子。
        /// </summary>
        /// <param name="from">起点。</param>
        /// <param name="to">终点。</param>
        /// <param name="duration">显示时长（秒）。</param>
        public void Play(Vector2 from, Vector2 to, float duration)
        {
            Vector3 start = new Vector3(from.x, from.y, VfxDepth);
            Vector3 end = new Vector3(to.x, to.y, VfxDepth);
            Vector3 delta = end - start;
            float distance = delta.magnitude;
            if (distance <= 0.01f)
            {
                return;
            }

            Vector3 direction = delta / distance;
            // Vefects Zap 锚点在落点，闪电沿本地 +Y 朝来源方向延伸（Demo 中为从地面向天空）。
            Vector3 directionToSource = -direction;
            transform.position = end;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, directionToSource);
            transform.localScale = new Vector3(1f, distance / BoltReferenceLength, 1f);

            gameObject.SetActive(true);
            RestartParticleSystems();
            remaining = duration;
        }

        /// <summary>每帧递减剩余显示时长，到期后隐藏特效。</summary>
        private void Update()
        {
            if (remaining <= 0f)
            {
                return;
            }

            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>重启所有子粒子系统，确保对象池复用时可见。</summary>
        private void RestartParticleSystems()
        {
            ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                ps.Clear(true);
                ps.Play(true);
            }
        }
    }
}
