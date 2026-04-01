using UnityEngine;

public class PlayerSkillManager : MonoBehaviour
{
    public SkillShoot sKillShoot { get; private set; }
    private void Awake()
    {
        sKillShoot = GetComponentInChildren<SkillShoot>();
    }
}
