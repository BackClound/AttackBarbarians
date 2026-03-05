using UnityEngine;

public class Player : Entity
{
    [SerializeField] private Transform attackCheck;
    //TODO 这个应该在工具类中根据手机的屏幕尺寸获取一个屏幕高度70% - 80%的距离
    private float attackCheckDistance = 50;

    #region Player State
    public StateMachine stateMachine { get; private set; }
    public PlayerIdleState idleState { get; private set; }
    public PlayerShootState shootState { get; private set; }
    public PlayerDeadState deadState { get; private set; }
    #endregion

    public override void Awake()
    {
        base.Awake();
        stateMachine = new StateMachine();
        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        shootState = new PlayerShootState(this, stateMachine, "Shoot");
        deadState = new PlayerDeadState(this, stateMachine, "Dead");

    }

    private void Start()
    {
        stateMachine.InitialState(idleState);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (attackCheck == null)
        {
            attackCheck = transform;
        }
        Gizmos.DrawWireSphere(attackCheck.position, attackCheckDistance);
    }

}
