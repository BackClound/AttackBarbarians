using UnityEngine;

/// <summary>
/// 射击技能兼容入口：委托 <see cref="SkillManager"/> 统一自动施法管线。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 Player 上，与 <see cref="SkillManager"/> 同物体。</para>
/// </remarks>
[DisallowMultipleComponent]
[RequireComponent(typeof(Player))]
public class ShootSkillController : MonoBehaviour
{
    [Header("Cast")]
    [SerializeField] private Transform castOrigin;
    [SerializeField] private ProjectileDataSO projectileDataOverride;

    private Player player;
    private PlayerController controller;
    private SkillManager skillManager;

    public Transform CastOrigin => castOrigin != null ? castOrigin : transform;

    private void Awake()
    {
        player = GetComponent<Player>();
        controller = GetComponent<PlayerController>();
        skillManager = GetComponent<SkillManager>();
        if (castOrigin == null)
        {
            castOrigin = transform;
        }
    }

    /// <summary>是否可释放射击（冷却就绪、已解锁、存在目标）。</summary>
    public bool CanShoot() => skillManager != null && skillManager.CanShootNow();

    /// <summary>手动触发一次射击，走与其他技能相同的 <see cref="SkillManager.TryCastSkill"/> 管线。</summary>
    public void ExecuteShoot()
    {
        skillManager?.TryCastSkill(SkillType.Shoot);
    }

    public float GetAnimSpeedMultiplier()
    {
        if (skillManager?.Context != null)
        {
            return skillManager.Context.GetAttackSpeedMultiplier();
        }

        if (controller != null && controller.RuntimeStats.IsInitialized)
        {
            return Mathf.Max(0.1f, controller.RuntimeStats.Get(StatType.AttackSpeedMulti));
        }

        return player != null && player.player_Health != null && player.player_Health.entity_Stats != null
            ? Mathf.Max(0.1f, player.player_Health.entity_Stats.GetAttackSpeedMultiplier())
            : 1f;
    }

    public ProjectileDataSO ProjectileDataOverride => projectileDataOverride;
}
