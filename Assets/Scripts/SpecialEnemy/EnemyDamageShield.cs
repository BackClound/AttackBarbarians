using UnityEngine;

/// <summary>
/// 敌人护盾：吸收伤害直至耗尽；由 <see cref="EnemyShieldAbility"/> 激活。
/// </summary>
/// <remarks><b>是否需要挂载：</b>可选，缺失时护盾能力会在运行时自动添加。</remarks>
[DisallowMultipleComponent]
public class EnemyDamageShield : MonoBehaviour
{
    private float remainingHp;

    public bool IsActive => remainingHp > 0f;
    public float RemainingHp => remainingHp;

    public void Activate(float shieldHp)
    {
        remainingHp = Mathf.Max(0f, shieldHp);
    }

    public void Clear()
    {
        remainingHp = 0f;
    }

    /// <summary>吸收伤害并返回实际扣血值。</summary>
    public float AbsorbDamage(float incomingDamage)
    {
        if (!IsActive || incomingDamage <= 0f)
        {
            return incomingDamage;
        }

        float absorbed = Mathf.Min(remainingHp, incomingDamage);
        remainingHp -= absorbed;
        return incomingDamage - absorbed;
    }

    private void OnDisable()
    {
        Clear();
    }
}
