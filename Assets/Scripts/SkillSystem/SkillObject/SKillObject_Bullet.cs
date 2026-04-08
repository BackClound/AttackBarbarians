using UnityEngine;

public class SKillObject_Bullet : SkillObject_Base
{
    #region 目标信息
    private Vector2 moveDirection;
    [SerializeField] private bool isStartAttacking;
    private Vector2 originalPosition;
    #endregion

    public override void Awake()
    {
        base.Awake();
        originalPosition = transform.position;
    }

    protected override void Update()
    {
        if (canMove)
        {
            if (moveDirection == Vector2.zero)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }
            rb.linearVelocity = moveDirection.normalized * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        Invoke("RecoverObjectStatus", 4f);
    }

    public override void SetupAttackObject(Vector2 moveDirection, AttackInfo info, float damage)
    {
        if (moveDirection == Vector2.zero)
        {
            Debug.LogError("Attack Object Setup Attack Object failed due to zero moveDirection");
            return;
        }
        this.moveDirection = moveDirection.normalized;
        canMove = true;
        //target will Take damagevalue, and update the realHp
        damageValue = damage;
        //将会对enemy进行攻击，对enemy的真实血量进行预更新
        // target.enemy_Health.WillReduceHp(damage);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        //获取接触到的可被攻击的敌人
        Debug.Log("Attack Object OnTriggerEnter2D  collision TAG = " + collision.gameObject.tag);
        if (collision != null)
        {
            var enemy = collision.GetComponent<Enemy>();
            Debug.Log("Attack Object OnTriggerEnter2D  enemy = " + enemy);
            if (enemy != null)
            {
                DoDamage(enemy, damageValue);
                RecoverObjectStatus();
            }

        }
    }

    override protected void RecoverObjectStatus()
    {
        canMove = false;
        damageValue = 0;
        transform.position = originalPosition;
        moveDirection = Vector2.zero;
        gameObject.SetActive(false);
    }

}
