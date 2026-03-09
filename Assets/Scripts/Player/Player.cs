using UnityEngine;

/// <summary>
/// 这个类应该控制Player本身的移动，动画，动效等行为
/// </summary>
public class Player : Entity
{
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

}
