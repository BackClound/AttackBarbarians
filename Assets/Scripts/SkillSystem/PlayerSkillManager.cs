using UnityEngine;

/// <summary>
/// 玩家技能入口：兼容旧 <see cref="SkillShoot"/>，并暴露 <see cref="SkillManager"/> / <see cref="ShootSkillController"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 Player 根物体上。</para>
/// </remarks>
public class PlayerSkillManager : MonoBehaviour
{
    /// <summary>Legacy 射击表现组件（子物体）。</summary>
    public SkillShoot sKillShoot { get; private set; }
    /// <summary>技能调度与自动施法管理器。</summary>
    public SkillManager SkillManager { get; private set; }
    /// <summary>射击兼容控制器。</summary>
    public ShootSkillController ShootController { get; private set; }
    /// <summary>Buff 应用门面。</summary>
    public BuffManager BuffManager { get; private set; }

    /// <summary>解析并确保挂载所需技能子系统组件。</summary>
    private void Awake()
    {
        sKillShoot = GetComponentInChildren<SkillShoot>();
        SkillManager = GetComponent<SkillManager>();
        if (SkillManager == null)
        {
            SkillManager = gameObject.AddComponent<SkillManager>();
        }

        ShootController = GetComponent<ShootSkillController>();
        if (ShootController == null)
        {
            ShootController = gameObject.AddComponent<ShootSkillController>();
        }

        BuffManager = GetComponent<BuffManager>();
        if (BuffManager == null)
        {
            BuffManager = gameObject.AddComponent<BuffManager>();
        }
    }

    /// <summary>
    /// 射击是否可释放。
    /// </summary>
    /// <returns>冷却就绪且有目标时返回 true。</returns>
    public bool CanShoot() => ShootController != null && ShootController.CanShoot();

    /// <summary>手动执行一次射击。</summary>
    public void ExecuteShoot() => ShootController?.ExecuteShoot();

    /// <summary>
    /// 应用技能 Buff（委托 <see cref="SkillManager"/>）。
    /// </summary>
    /// <param name="kind">Buff 种类。</param>
    /// <param name="tier">Buff 层级，默认 1。</param>
    public void ApplySkillBuff(SkillBuffKind kind, int tier = 1)
    {
        SkillManager?.ApplySkillBuff(kind, tier);
    }

    /// <summary>
    /// 应用配置 Buff（委托 <see cref="BuffManager"/>）。
    /// </summary>
    /// <param name="buff">Buff 配置资产。</param>
    /// <param name="stacks">堆叠层数，默认 1。</param>
    public void ApplyBuff(BuffDataSO buff, int stacks = 1)
    {
        BuffManager?.ApplyBuff(buff, stacks);
    }
}
