using UnityEngine;

/// <summary>
/// 玩家技能入口：兼容旧 <see cref="SkillShoot"/>，并暴露 <see cref="SkillManager"/>。
/// </summary>
public class PlayerSkillManager : MonoBehaviour
{
    public SkillShoot sKillShoot { get; private set; }
    public SkillManager SkillManager { get; private set; }
    public BuffManager BuffManager { get; private set; }

    private void Awake()
    {
        sKillShoot = GetComponentInChildren<SkillShoot>();
        SkillManager = GetComponent<SkillManager>();
        if (SkillManager == null)
        {
            SkillManager = gameObject.AddComponent<SkillManager>();
        }

        BuffManager = GetComponent<BuffManager>();
        if (BuffManager == null)
        {
            BuffManager = gameObject.AddComponent<BuffManager>();
        }
    }

    public void ApplySkillBuff(SkillBuffKind kind, int tier = 1)
    {
        SkillManager?.ApplySkillBuff(kind, tier);
    }

    public void ApplyBuff(BuffDataSO buff, int stacks = 1)
    {
        BuffManager?.ApplyBuff(buff, stacks);
    }
}
