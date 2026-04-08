using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShootWaveData
{
    public List<SKillObject_Bullet> bulletList;

    private int shootLine = 1;
    public int currentBulletIndex = 0;

    public ShootWaveData(List<SKillObject_Bullet> bulletList, int shootLine)
    {
        this.bulletList = bulletList;
        this.shootLine = shootLine;
    }

    public void AddBullet(SKillObject_Bullet bullet)
    {
        bulletList.Add(bullet);
        currentBulletIndex++;
    }

    public void UpdateBulletListByLine(int shootLine, GameObject bullet)
    {
        this.shootLine = shootLine;
        if (currentBulletIndex >= shootLine)
        {
            return;
        }
        else
        {
            for (int j = currentBulletIndex; j < shootLine; j++)
            {
                bullet.SetActive(false);
                AddBullet(bullet.GetComponent<SKillObject_Bullet>());
            }
        }

    }

}
