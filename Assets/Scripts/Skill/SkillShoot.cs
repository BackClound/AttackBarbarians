using UnityEngine;

//在level等级中，设置两列/三列/两排/三排/换弹时发射散弹或者从左到右的一排子弹
public class SkillShoot : SkillBase
{
    [SerializeField] private GameObject shootPrefab;
    [SerializeField] private int shootCount;
    [SerializeField] private int shootLine;
    [SerializeField] private int shootWave;
    private float spaceBetweenBulletLine;
    private float spaceBetweenBulletWave;
    private GameObject[] bullets;

    [SerializeField] private float attackInterval;
    [SerializeField] private int currentAttackTime = 0;

    private void InitialBulletList()
    {

    }

    public void ActivateBullet(GameObject bullet)
    {

    }

    private void InactivateBullet(GameObject bullet)
    {

    }

}
