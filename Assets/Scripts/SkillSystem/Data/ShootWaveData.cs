using System;
using System.Collections.Generic;

/// <summary>
/// 射击波次数据（Legacy 多排弹道扩展预留）。
/// </summary>
[Serializable]
public class ShootWaveData
{
    /// <summary>本波次已注册的投射物列表。</summary>
    public List<ProjectileController> projectileList;

    /// <summary>本波次弹道行数。</summary>
    private int shootLine = 1;
    /// <summary>当前投射物索引计数。</summary>
    public int currentProjectileIndex;

    /// <summary>
    /// 初始化波次数据。
    /// </summary>
    /// <param name="projectileList">投射物列表容器。</param>
    /// <param name="shootLine">弹道行数。</param>
    public ShootWaveData(List<ProjectileController> projectileList, int shootLine)
    {
        this.projectileList = projectileList;
        this.shootLine = shootLine;
    }

    /// <summary>
    /// 向本波次追加一枚投射物并递增索引。
    /// </summary>
    /// <param name="projectile">待追加的投射物控制器。</param>
    public void AddProjectile(ProjectileController projectile)
    {
        projectileList.Add(projectile);
        currentProjectileIndex++;
    }
}
