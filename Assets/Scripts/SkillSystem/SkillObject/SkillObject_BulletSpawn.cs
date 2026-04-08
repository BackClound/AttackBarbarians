using System.Collections.Generic;
using UnityEngine;

public class SkillObject_BulletSpawn : MonoBehaviour
{

    //TODO 这是Player当前的武器，可以是发射子弹/激光/或者其他的
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private List<SKillObject_Bullet> bulletList;
    [SerializeField] private int maxBullets = 1;

    [Header("弹道信息")]
    [SerializeField] private int shootLine = 1;
    [SerializeField] private float shootAngle = 5;

    [Header("多波次攻击")]
    private float spaceBetweenBulletLine;
    private float spaceBetweenBulletWave;
    private Player player;

    public void SetupBulletSpawn(Player player, int maxBullets)
    {
        this.player = player;
        this.maxBullets = maxBullets;
        if (bulletList == null || bulletList.Count == 0)
        {
            bulletList = new List<SKillObject_Bullet>();
            InitialBulletList();
        }
    }

    private void InitialBulletList()
    {
        for (int i = 0; i < maxBullets; i++)
        {
            GameObject bullet = CreateBulletPre();
            bullet.SetActive(false);
            var bulletComponent = bullet.GetComponent<SKillObject_Bullet>();
            bulletList.Add(bulletComponent);
        }
    }

    private GameObject CreateBulletPre()
    {
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        return bullet;
    }

    public void UpdateMaxBullets(int newMaxBullets)
    {
        maxBullets = newMaxBullets;
        if (bulletList.Count < maxBullets)
        {
            int bulletsToAdd = maxBullets - bulletList.Count;
            for (int i = 0; i < bulletsToAdd; i++)
            {
                GameObject bullet = CreateBulletPre();
                bullet.SetActive(false);
                var bulletComponent = bullet.GetComponent<SKillObject_Bullet>();
                bulletList.Add(bulletComponent);
            }
        }
        else if (bulletList.Count > maxBullets)
        {
            int bulletsToRemove = bulletList.Count - maxBullets;
            for (int i = 0; i < bulletsToRemove; i++)
            {
                var bulletToRemove = bulletList[bulletList.Count - 1];
                bulletList.RemoveAt(bulletList.Count - 1);
                Destroy(bulletToRemove.gameObject);
            }
        }
    }

    public void ApplyBulletWithEnemy(Enemy enemy)
    {
        //TODO 根据bulletIndexInLine和shootLine计算bullet的发射角度，调整bullet的朝向
        int middleIndex = maxBullets / 2;

        float batchOffset = bulletList.Count > 1 ? 10f : 0f;
        Vector2 baseDirection = (enemy.transform.position - transform.position).normalized;
        float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;

        for (int i = 0; i < bulletList.Count; i++)
        {
            Transform bullet = bulletList[i].transform;
            float angleOffset = (i - middleIndex) * batchOffset;
            float angle = baseAngle + angleOffset;
            bullet.rotation = Quaternion.Euler(0, 0, angle);
            Vector2 forwardDir = bullet.right;
            bulletList[i].SetupAttackObject(forwardDir, null, player.player_Health.entity_Stats.GetTotalDamage());
            bullet.gameObject.SetActive(true);
        }
    }
}
