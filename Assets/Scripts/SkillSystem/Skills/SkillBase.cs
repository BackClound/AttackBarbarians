using Unity.VisualScripting;
using UnityEngine;

public class SkillBase : MonoBehaviour
{
    protected float coolDownTime;
    protected float latestUsedTime;

    protected SkillLevelData skillData;

    protected virtual void Awake()
    {
        latestUsedTime = Time.time - coolDownTime;
    }

    protected virtual void Update()
    {

    }

    public void UnlockSkillLevelToNewLevel(SkillLevelData levelData)
    {
        coolDownTime = coolDownTime * (1 - levelData.skillScaleData.coolDownScaleMulti);
        this.skillData = levelData;
    }

    public bool CanUseSkill()
    {
        return Time.time > latestUsedTime + coolDownTime;
    }

    public void StartCoolDown() => latestUsedTime = Time.time;

    [ContextMenu("Reset Skill Cool Down Time")]
    public void ResetCoolDownTime() => latestUsedTime = Time.time;

}
