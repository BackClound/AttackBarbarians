using UnityEngine;

public class EnemyCombatManager : EntityCombat
{
    private Enemy enemy;
    private WallControlManager wallControl;
    protected override void Awake()
    {
        wallControl = WallControlManager.sInstance;
        enemy = GetComponent<Enemy>();
    }

    public override void PerformAttack()
    {
        RaycastHit2D hit2D = Physics2D.Raycast(checkPosition.position, Vector2.down, maxCheckDistance, enemyLayer);
        wallControl = hit2D.collider.GetComponent<WallControlManager>();
        if (wallControl != null)
        {
            wallControl.TakeDamage(enemy.GetDamageValue());
        }
    }
}
