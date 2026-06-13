using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Legacy 技能基类：本地冷却计时与等级解锁；新技能请使用 <see cref="SkillManager"/> + <see cref="ISkillEffect"/> 管线。
/// </summary>
public class SkillBase : MonoBehaviour
{
    /// <summary>本地冷却秒数。</summary>
    protected float coolDownTime;
    /// <summary>上次使用技能的时间戳（<see cref="Time.time"/>）。</summary>
    protected float latestUsedTime;

    /// <summary>当前等级数据与缩放系数。</summary>
    protected SkillLevelData skillData;

    /// <summary>初始化冷却计时，使首帧即可使用。</summary>
    protected virtual void Awake()
    {
        latestUsedTime = Time.time - coolDownTime;
    }

    /// <summary>每帧更新（子类可重写）。</summary>
    protected virtual void Update()
    {

    }

    /// <summary>
    /// 解锁到新等级并应用冷却缩放。
    /// </summary>
    /// <param name="levelData">等级数据与缩放系数。</param>
    public void UnlockSkillLevelToNewLevel(SkillLevelData levelData)
    {
        coolDownTime = coolDownTime * (1 - levelData.skillScaleData.coolDownScaleMulti);
        this.skillData = levelData;
    }

    /// <summary>
    /// 本地冷却是否就绪。
    /// </summary>
    /// <returns>是否可以使用技能。</returns>
    public bool CanUseSkill()
    {
        return Time.time > latestUsedTime + coolDownTime;
    }

    /// <summary>启动本地冷却计时。</summary>
    public void StartCoolDown() => latestUsedTime = Time.time;

    /// <summary>重置冷却，使技能立即可用（Inspector 调试）。</summary>
    [ContextMenu("Reset Skill Cool Down Time")]
    public void ResetCoolDownTime() => latestUsedTime = Time.time;

}
