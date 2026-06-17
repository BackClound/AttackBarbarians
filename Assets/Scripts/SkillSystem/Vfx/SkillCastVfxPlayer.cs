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

    private static bool HasLinkComponent(GameObject prefabRoot)
    {
        return prefabRoot.GetComponent<SkillLightningLinkVfx>() != null
            || prefabRoot.GetComponentInChildren<SkillLightningLinkVfx>(true) != null;
    }

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

    private sealed class SkillLightningBolt : MonoBehaviour
    {
        private SkillLightningLinkVfx linkVfx;
        private float remaining;

        public bool IsActive => remaining > 0f;

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
