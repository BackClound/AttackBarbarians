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

    private Animator anim;
    [SerializeField] private bool beDamaged;

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
    }

    public void TakeDamage(float damage)
    {
        if (currentHP > 0)
        {
            currentHP -= damage;
        }
        if (currentHP <= 0)
        {
            Debug.Log("游戏结束，玩家失败！！！");
        }
    }
}
