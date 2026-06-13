using UnityEngine;

/// <summary>
/// 敌人护盾：吸收伤害直至耗尽；由 <see cref="EnemyShieldAbility"/> 激活。
/// </summary>
/// <remarks><b>是否需要挂载：</b>可选，缺失时护盾能力会在运行时自动添加。</remarks>
[DisallowMultipleComponent]
public class EnemyDamageShield : MonoBehaviour
{
    private float remainingHp;

    /// <summary>护盾是否仍处于激活状态。</summary>
    public bool IsActive => remainingHp > 0f;
    /// <summary>护盾剩余吸收量。</summary>
    public float RemainingHp => remainingHp;

    /// <summary>
    /// 激活护盾并设置吸收量。
    /// </summary>
    /// <param name="shieldHp">护盾生命值。</param>
    public void Activate(float shieldHp)
    {
        remainingHp = Mathf.Max(0f, shieldHp);
    }

    /// <summary>清空护盾。</summary>
    public void Clear()
    {
        remainingHp = 0f;
    }

    /// <summary>
    /// 吸收伤害并返回穿透后的实际伤害。
    /// </summary>
    /// <param name="incomingDamage">入射伤害量。</param>
    /// <returns>护盾吸收后剩余伤害。</returns>
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

    /// <summary>禁用时自动清空护盾。</summary>
    private void OnDisable()
    {
        Clear();
    }
}
