using UnityEngine;

/// <summary>
/// 城墙表现与受击代理：将敌人伤害转发到 Player 血量，并驱动墙体动画状态机。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在场景墙体物体上。</para>
/// <para><b>获取方式：</b><see cref="Instance"/> 或旧名 <see cref="sInstance"/>（不再使用 Find 懒查找）。</para>
/// </remarks>
public class WallControlManager : MonoBehaviour
{
    public static WallControlManager sInstance => Instance;

    public static WallControlManager Instance => SingletonHost<WallControlManager>.Instance;

    public static bool HasInstance => SingletonHost<WallControlManager>.HasInstance;

    private Player_Health playerHealth
    {
        get
        {
            if (Player.HasInstance)
            {
                return Player.Instance.player_Health;
            }
            return null;
        }
    }

    public Animator anim;
    [SerializeField] public bool beDamaged;

    #region 
    public StateMachine stateMachine;
    public WallIdleState idleState;
    public WallDamageState damageState;
    #endregion


    private void Awake()
    {
        if (!SingletonHost<WallControlManager>.TryClaim(this, this, SingletonOptions.SceneDefault, out bool destroyedOwner) || destroyedOwner)
        {
            return;
        }

        anim = GetComponent<Animator>();
        beDamaged = false;

        stateMachine = new StateMachine();
        idleState = new WallIdleState(this, stateMachine, "isIdle");
        damageState = new WallDamageState(this, stateMachine, "isDamaged");
    }

    private void OnDestroy()
    {
        SingletonHost<WallControlManager>.Release(this);
    }

    private void Start()
    {
        stateMachine.InitialState(idleState);
    }
    private void Update()
    {
        stateMachine.currentState.OnUpdate();
    }

    /// <summary>
    /// TODO 在同一帧内，当有多个enemy同时进行了攻击，并且调用了TakeDamage方法的话，考虑同步的问题，避免只有一次damage生效
    /// </summary>
    /// <param name="damage"></param>
    public void TakeDamage(float damage)
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
        beDamaged = true;
    }

    public void ResetDamageState(bool isDamaged)
    {
        beDamaged = isDamaged;
    }

    public void OnAnimFinished()
    {
        if (stateMachine.currentState == null) return;
        stateMachine.currentState.OnAnimFinished();
    }


}
