using UnityEditor.Toolbars;
using UnityEngine;

/// <summary>
/// 这个类应该控制Player本身的移动，动画，动效等行为
/// </summary>
public class Player : Entity
{

    public static Player sInstance { get; private set; }

    #region Player other Controlers
    public Player_Health player_Health { get; private set; }
    public PlayerSkillManager skillManager { get; private set; }
    public PlayerCombat playerCombatManager { get; private set; }
    #endregion

    #region Player State
    public PlayerIdleState idleState { get; private set; }
    public PlayerShootState shootState { get; private set; }
    public PlayerDeadState deadState { get; private set; }
    #endregion

    public override void Awake()
    {
        base.Awake();
        if (sInstance != null && sInstance != this)
        {
            Destroy(gameObject);
            return;
        }
        sInstance = this;
        stateMachine = new StateMachine();
        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        shootState = new PlayerShootState(this, stateMachine, "Shoot");
        deadState = new PlayerDeadState(this, stateMachine, "Dead");

        player_Health = GetComponent<Player_Health>();
        skillManager = GetComponent<PlayerSkillManager>();
        playerCombatManager = GetComponent<PlayerCombat>();
    }

    public override void Start()
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

    public override void OnAniamtorFinished()
    {
        // Debug.Log("Player trigger OnAniamtorFinished");

        stateMachine.currentState.OnAnimFinished();
    }

    public void Die()
    {
        stateMachine.ChangeState(deadState);
    }

    public void OnAnimatorAttackTrigger()
    {
        // Debug.Log("Player trigger OnAnimEventTrigger");
        stateMachine.currentState.OnAnimAttackTrigger();
    }
}
