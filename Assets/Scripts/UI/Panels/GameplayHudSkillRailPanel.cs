using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗 HUD 右侧已解锁技能栏。
/// </summary>
public class GameplayHudSkillRailPanel : MonoBehaviour
{
    [SerializeField] private GameplaySkillSlotView[] skillSlots;
    [SerializeField] private int maxVisibleSlots = 6;

    private readonly List<SkillRuntime> visibleRuntimes = new List<SkillRuntime>(8);
    private float refreshTimer;
    private const float CooldownRefreshInterval = 0.12f;

    /// <summary>订阅技能解锁/升级事件。</summary>
    private void OnEnable()
    {
        GameEvents.SubscribeSkillUnlocked(OnSkillChanged);
        GameEvents.Subscribe(GameConstants.EventKeys.SkillLevelUp, OnSkillChanged);
        GameEvents.SubscribeBuffChanged(OnSkillChanged);
        GameEvents.SubscribeGameStarted(OnGameStarted);
        RefreshAll();
    }

    /// <summary>取消事件订阅。</summary>
    private void OnDisable()
    {
        GameEvents.UnsubscribeSkillUnlocked(OnSkillChanged);
        GameEvents.Unsubscribe(GameConstants.EventKeys.SkillLevelUp, OnSkillChanged);
        GameEvents.UnsubscribeBuffChanged(OnSkillChanged);
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
    }

    /// <summary>周期性刷新冷却遮罩。</summary>
    private void Update()
    {
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer >= CooldownRefreshInterval)
        {
            refreshTimer = 0f;
            RefreshCooldownsOnly();
        }
    }

    /// <summary>游戏开始时全量刷新技能栏。</summary>
    /// <param name="ctx">游戏开始事件上下文。</param>
    private void OnGameStarted(GameEventContext ctx) => RefreshAll();

    /// <summary>技能解锁、升级或 Buff 变更时全量刷新。</summary>
    /// <param name="ctx">相关事件上下文。</param>
    private void OnSkillChanged(GameEventContext ctx) => RefreshAll();

    /// <summary>全量刷新技能槽列表。</summary>
    public void RefreshAll()
    {
        visibleRuntimes.Clear();
        if (skillSlots == null || skillSlots.Length == 0)
        {
            return;
        }

        if (PlayerSceneAccess.TryGetSkillSystem(out SkillManager skillManager))
        {
            foreach (KeyValuePair<SkillType, SkillRuntime> pair in skillManager.Runtimes)
            {
                SkillRuntime runtime = pair.Value;
                if (runtime != null && runtime.IsUnlocked && runtime.Config != null)
                {
                    visibleRuntimes.Add(runtime);
                }
            }

            visibleRuntimes.Sort((a, b) => a.Config.SkillType.CompareTo(b.Config.SkillType));
        }

        int slotCount = skillSlots.Length;
        int visibleCount = Mathf.Min(slotCount, maxVisibleSlots, visibleRuntimes.Count);
        for (int i = 0; i < slotCount; i++)
        {
            GameplaySkillSlotView slot = skillSlots[i];
            if (slot == null)
            {
                continue;
            }

            if (i < visibleCount)
            {
                slot.SetData(visibleRuntimes[i]);
            }
            else
            {
                slot.SetEmpty();
            }
        }
    }

    /// <summary>仅刷新可见槽位的冷却遮罩（轻量周期更新）。</summary>
    private void RefreshCooldownsOnly()
    {
        if (skillSlots == null)
        {
            return;
        }

        int visibleCount = Mathf.Min(skillSlots.Length, maxVisibleSlots, visibleRuntimes.Count);
        for (int i = 0; i < visibleCount; i++)
        {
            GameplaySkillSlotView slot = skillSlots[i];
            if (slot != null && slot.gameObject.activeSelf)
            {
                slot.SetData(visibleRuntimes[i]);
            }
        }
    }
}
