using UnityEngine;

public class WallControlManager : MonoBehaviour
{
    private static WallControlManager _sInstance;
    public static WallControlManager sInstance
    {
        get
        {
            if (_sInstance == null)
            {
                _sInstance = FindFirstObjectByType<WallControlManager>();
            }
            return _sInstance;
        }
    }

    [SerializeField] private float maxHP;

    [SerializeField] private float currentHP;

    public Animator anim;
    [SerializeField] public bool beDamaged;

    #region 
    public StateMachine stateMachine;
    public WallIdleState idleState;
    public WallDamageState damageState;
    #endregion


    private void Awake()
    {
        if (_sInstance != null && _sInstance != this)
        {
            Destroy(gameObject);
            return;
        }
        anim = GetComponent<Animator>();
        beDamaged = false;
        _sInstance = this;

        stateMachine = new StateMachine();
        idleState = new WallIdleState(this, stateMachine, "isIdle");
        damageState = new WallDamageState(this, stateMachine, "isDamaged");
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
        if (currentHP > 0)
        {
            currentHP -= damage;
            beDamaged = true;
        }
        if (currentHP <= 0)
        {
            Debug.Log("游戏结束，玩家失败！！！");
        }
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
