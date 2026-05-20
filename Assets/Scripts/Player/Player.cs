using UnityEngine;

/// <summary>
/// 玩家实体：动画回调、状态实例与单例；状态机 Tick 由 <see cref="PlayerController"/> 驱动。
/// </summary>
public class Player : Entity
{
    /// <summary>场景内玩家单例。旧代码请逐步改用 <see cref="Instance"/>。</summary>
    public static Player sInstance => Instance;

    public static Player Instance => SingletonHost<Player>.Instance;

    public static bool HasInstance => SingletonHost<Player>.HasInstance;

    #region Player other Controlers
    public PlayerController controller { get; private set; }
    public Player_Health player_Health { get; private set; }
    public PlayerSkillManager skillManager { get; private set; }
    public PlayerCombatBridge combatBridge { get; private set; }
    #endregion

    #region Player State
    public PlayerIdleState idleState { get; private set; }
    public PlayerShootState shootState { get; private set; }
    public PlayerDeadState deadState { get; private set; }
    #endregion

    public override void Awake()
    {
        base.Awake();
        if (!SingletonHost<Player>.TryClaim(this, this, SingletonOptions.SceneDefault, out bool destroyedOwner) || destroyedOwner)
        {
            return;
        }

        stateMachine = new StateMachine();
        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        shootState = new PlayerShootState(this, stateMachine, "Shoot");
        deadState = new PlayerDeadState(this, stateMachine, "Dead");

        controller = GetComponent<PlayerController>();
        player_Health = GetComponent<Player_Health>();
        skillManager = GetComponent<PlayerSkillManager>();
        combatBridge = GetComponent<PlayerCombatBridge>();
        if (combatBridge == null)
        {
            combatBridge = GetComponent<PlayerCombat>();
        }
    }

    public override void Start()
    {
        stateMachine.InitialState(idleState);
    }

    public override void OnAniamtorFinished()
    {
        stateMachine.currentState?.OnAnimFinished();
    }

    public void Die()
    {
        stateMachine.ChangeState(deadState);
    }

    public void OnAnimatorAttackTrigger()
    {
        stateMachine.currentState?.OnAnimAttackTrigger();
    }

    private void OnDestroy()
    {
        SingletonHost<Player>.Release(this);
    }
}
