using UnityEngine;

/// <summary>
/// 玩家技能入口：兼容旧 <see cref="SkillShoot"/>，并暴露 <see cref="SkillManager"/> / <see cref="ShootSkillController"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 Player 根物体上。</para>
/// </remarks>
public class PlayerSkillManager : MonoBehaviour
{
    public SkillShoot sKillShoot { get; private set; }
    public SkillManager SkillManager { get; private set; }
    public ShootSkillController ShootController { get; private set; }
    public BuffManager BuffManager { get; private set; }

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

    public bool CanShoot() => ShootController != null && ShootController.CanShoot();

    public void ExecuteShoot() => ShootController?.ExecuteShoot();

    public void ApplySkillBuff(SkillBuffKind kind, int tier = 1)
    {
        SkillManager?.ApplySkillBuff(kind, tier);
    }

    public void ApplyBuff(BuffDataSO buff, int stacks = 1)
    {
        BuffManager?.ApplyBuff(buff, stacks);
    }
}
