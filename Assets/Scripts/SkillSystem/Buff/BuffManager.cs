using UnityEngine;

/// <summary>
/// Buff 应用门面：技能 Buff 走 <see cref="SkillManager"/>，属性 Buff 走 <see cref="PlayerController"/>。
/// 流水线位置：Upgrade/<see cref="BuffDataSO"/> → 本类 → SkillManager 或 PlayerController。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 Player 或 GameSystems；Inspector 可指定 Player 引用。</para>
/// </remarks>
public class BuffManager : MonoBehaviour
{
    [SerializeField] private Player playerOverride;
    [SerializeField] private SkillManager skillManagerOverride;

    private SkillManager skillManager;
    private PlayerController playerController;

    /// <summary>解析 SkillManager 与 PlayerController 引用。</summary>
    private void Awake()
    {
        ResolveReferences();
    }

    /// <summary>
    /// 应用 Buff 配置（技能 Buff 或玩家属性 Buff）。
    /// </summary>
    /// <param name="buff">Buff 数据配置。</param>
    /// <param name="stacks">堆叠层数，默认 1。</param>
    /// <param name="source">技能 Buff 应用来源。</param>
    public void ApplyBuff(BuffDataSO buff, int stacks = 1, SkillBuffApplySource source = SkillBuffApplySource.RunUpgrade)
    {
        if (buff == null)
        {
            return;
        }

        ResolveReferences();
        if (buff.HasSkillBuff && skillManager != null)
        {
            skillManager.ApplyBuffFromConfig(buff, stacks, source);
            GameEvents.RaiseBuffApplied(this, new BuffEventArgs(
                buff.ConfigId,
                stacks,
                buff.IsPermanent ? -1f : buff.Duration,
                playerOverride != null ? playerOverride : playerController?.Player));
            return;
        }

        if (playerController != null)
        {
            playerController.ApplyBuff(buff, stacks);
            GameEvents.RaiseBuffApplied(this, new BuffEventArgs(
                buff.ConfigId,
                stacks,
                buff.IsPermanent ? -1f : buff.Duration,
                playerController.Player));
        }
    }

    /// <summary>
    /// 直接应用技能 Buff（不经过 BuffDataSO）。
    /// </summary>
    /// <param name="kind">技能 Buff 种类。</param>
    /// <param name="tier">Buff 层级，默认 1。</param>
    public void ApplySkillBuff(SkillBuffKind kind, int tier = 1)
    {
        ResolveReferences();
        skillManager?.ApplySkillBuff(kind, tier);
    }

    /// <summary>懒解析 SkillManager 与 PlayerController（支持 Override 与场景访问）。</summary>
    private void ResolveReferences()
    {
        if (skillManager == null)
        {
            skillManager = skillManagerOverride;
            if (skillManager == null && playerOverride != null)
            {
                skillManager = playerOverride.GetComponent<SkillManager>();
            }

            if (skillManager == null)
            {
                PlayerSceneAccess.TryGetSkillSystem(out skillManager);
            }
        }

        if (playerController == null)
        {
            Player player = playerOverride;
            if (player == null && skillManager != null)
            {
                player = skillManager.GetComponent<Player>();
            }

            if (player == null)
            {
                PlayerSceneAccess.TryGetPlayer(out player);
            }

            playerController = player != null ? player.controller : null;
        }
    }
}
