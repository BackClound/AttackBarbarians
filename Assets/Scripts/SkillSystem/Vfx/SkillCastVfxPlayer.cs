using UnityEngine;

/// <summary>
/// 技能施法特效播放器：链状闪电使用 Shader 四边形（<see cref="SkillLightningLinkVfx"/>）。
/// </summary>
/// <remarks><b>是否需要挂载：</b>否。静态调用。</remarks>
public static class SkillCastVfxPlayer
{
    private const float DefaultBoltDuration = 2f;
    private const int MaxActiveBolts = 24;
    private const string LinkPrefabResourcePath = "VFX/Skill/CopyLightning";

    private static GameObject cachedLinkPrefab;

    private static readonly SkillLightningBolt[] BoltPool = new SkillLightningBolt[MaxActiveBolts];
    private static int boltPoolCount;

    /// <summary>在两点间播放闪电链特效。</summary>
    public static void PlayLightningSegment(Vector2 from, Vector2 to, GameObject prefab, int buffTier = 1)
    {
        GameObject linkPrefab = ResolveLinkPrefab(prefab);
        if (linkPrefab == null)
        {
            Debug.LogWarning(
                "SkillCastVfxPlayer: 未找到链状闪电 Prefab，请执行 Attack Barbarians/VFX/Create Copy Lightning Prefab。");
            return;
        }

        SkillLightningBolt bolt = RentBolt(linkPrefab);
        bolt.Play(from, to, DefaultBoltDuration);
    }

    /// <summary>解析可用的闪电链 Prefab（优先入参，否则 Resources 缓存）。</summary>
    /// <param name="prefab">调用方指定的 Prefab，可为 null。</param>
    /// <returns>带 <see cref="SkillLightningLinkVfx"/> 的 Prefab；未找到时返回 null。</returns>
    private static GameObject ResolveLinkPrefab(GameObject prefab)
    {
        if (prefab != null && HasLinkComponent(prefab))
        {
            return prefab;
        }

        if (cachedLinkPrefab == null)
        {
            cachedLinkPrefab = Resources.Load<GameObject>(LinkPrefabResourcePath);
        }

        return cachedLinkPrefab;
    }

    /// <summary>检查 Prefab 根或子节点是否包含闪电链组件。</summary>
    /// <param name="prefabRoot">Prefab 根物体。</param>
    private static bool HasLinkComponent(GameObject prefabRoot)
    {
        return prefabRoot.GetComponent<SkillLightningLinkVfx>() != null
            || prefabRoot.GetComponentInChildren<SkillLightningLinkVfx>(true) != null;
    }

    /// <summary>从对象池租用或新建一条闪电实例。</summary>
    /// <param name="prefab">闪电链 Prefab。</param>
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
            host.name = "SkillLightningLinkVfx";
            Object.DontDestroyOnLoad(host);
            host.SetActive(false);
            var bolt = host.AddComponent<SkillLightningBolt>();
            BoltPool[boltPoolCount++] = bolt;
            return bolt;
        }

        return BoltPool[0];
    }

    /// <summary>单条闪电链实例的运行时控制器（对象池元素）。</summary>
    private sealed class SkillLightningBolt : MonoBehaviour
    {
        private SkillLightningLinkVfx linkVfx;
        private float remaining;

        public bool IsActive => remaining > 0f;

        /// <summary>在两点间激活闪电并启动倒计时。</summary>
        /// <param name="from">起点（世界坐标）。</param>
        /// <param name="to">终点（世界坐标）。</param>
        /// <param name="duration">显示时长（秒）。</param>
        public void Play(Vector2 from, Vector2 to, float duration)
        {
            if (linkVfx == null)
            {
                linkVfx = GetComponent<SkillLightningLinkVfx>();
                if (linkVfx == null)
                {
                    linkVfx = GetComponentInChildren<SkillLightningLinkVfx>(true);
                }
            }

            if (linkVfx == null)
            {
                Debug.LogWarning("SkillCastVfxPlayer: 闪电实例缺少 SkillLightningLinkVfx 组件。");
                return;
            }

            const float depth = -1f;
            linkVfx.ApplyLink(from, to, depth);
            gameObject.SetActive(true);
            linkVfx.PlayVisuals(duration);
            remaining = duration;
        }

        /// <summary>每帧递减剩余显示时间，到期后隐藏实例。</summary>
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
    }
}
