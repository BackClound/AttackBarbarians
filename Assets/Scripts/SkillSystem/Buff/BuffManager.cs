using UnityEngine;

/// <summary>
/// Buff 应用门面：技能 Buff 走 <see cref="SkillManager"/>，属性 Buff 走 <see cref="PlayerController"/>。
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

    private void Awake()
    {
        ResolveReferences();
    }
/// <summary>
/// 应用 Buff
/// </summary>
/// <param name="buff">Buff 数据</param>
/// <param name="stacks">堆叠层数</param>
    public void ApplyBuff(BuffDataSO buff, int stacks = 1)
    {
        if (buff == null)
        {
            return;
        }

        ResolveReferences();
        if (buff.HasSkillBuff && skillManager != null)
        {
            skillManager.ApplyBuffFromConfig(buff, stacks);
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
/// 应用技能 Buff
/// </summary>
/// <param name="kind">技能 Buff 种类</param>
/// <param name="tier">技能 Buff 层级</param>
    public void ApplySkillBuff(SkillBuffKind kind, int tier = 1)
    {
        ResolveReferences();
        skillManager?.ApplySkillBuff(kind, tier);
    }

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
