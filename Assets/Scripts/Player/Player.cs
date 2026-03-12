using UnityEngine;

/// <summary>
/// 这个类应该控制Player本身的移动，动画，动效等行为
/// </summary>
public class Player : Entity
{
    public Player_Stats player_Stats { get; private set; }
    public PlayerSkillManager skillManager { get; private set; }
    public PlayerCombat playerCombatManager { get; private set; }

    #region Player State
    public StateMachine stateMachine { get; private set; }
    public PlayerIdleState idleState { get; private set; }
    public PlayerShootState shootState { get; private set; }
    public PlayerDeadState deadState { get; private set; }
    #endregion

    public override void Awake()
    {
        base.Awake();

        player_Stats = GetComponent<Player_Stats>();
        skillManager = GetComponent<PlayerSkillManager>();
        playerCombatManager = GetComponent<PlayerCombat>();

        stateMachine = new StateMachine();
        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        shootState = new PlayerShootState(this, stateMachine, "Shoot");
        deadState = new PlayerDeadState(this, stateMachine, "Dead");
    }

    private void Start()
    {
        stateMachine.InitialState(idleState);
    }

    private void Update()
    {
        stateMachine.currentState?.OnUpdate();
    }

    private void FixedUpdate()
    {
        stateMachine.currentState?.OnFixedUpdate();
    }

    public void OnAniamtorFinished()
    {
        Debug.Log("Player trigger OnAniamtorFinished");

        stateMachine.currentState.OnAnimFinished();
    }

    public void OnAnimatorEventTrigger()
    {
        Debug.Log("Player trigger OnAnimEventTrigger");
        stateMachine.currentState.OnAnimEventTrigger();
    }
}
