using System;
using System.Collections.Generic;

/// <summary>
/// 射击波次数据（预留多排弹道扩展）。
/// </summary>
[Serializable]
public class ShootWaveData
{
    public List<ProjectileController> projectileList;

    private int shootLine = 1;
    public int currentProjectileIndex;

    public ShootWaveData(List<ProjectileController> projectileList, int shootLine)
    {
        this.projectileList = projectileList;
        this.shootLine = shootLine;
    }

    public void AddProjectile(ProjectileController projectile)
    {
        projectileList.Add(projectile);
        currentProjectileIndex++;
    }
}
