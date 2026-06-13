using UnityEngine;

/// <summary>
/// 敌人控制类状态：麻痹、冰冻、减速；由技能系统写入，<see cref="EnemyState"/> 读取移速倍率。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在需要受控状态影响的敌人 Prefab 根节点。</para>
/// </remarks>
[DisallowMultipleComponent]
public class EnemyStatusController : MonoBehaviour
{
    private float stunUntil;
    private float freezeUntil;
    private float slowUntil;
    private float slowMoveMultiplier = 1f;

    /// <summary>是否处于麻痹状态。</summary>
    public bool IsStunned => Time.time < stunUntil;
    /// <summary>是否处于冰冻状态。</summary>
    public bool IsFrozen => Time.time < freezeUntil;
    /// <summary>是否被禁止移动（麻痹或冰冻）。</summary>
    public bool IsMovementBlocked => IsStunned || IsFrozen;

    /// <summary>当前移速倍率（受减速影响，被禁止移动时为 0）。</summary>
    public float MoveSpeedMultiplier
    {
        get
        {
            if (IsMovementBlocked)
            {
                return 0f;
            }

            return Time.time < slowUntil ? slowMoveMultiplier : 1f;
        }
    }

    /// <summary>
    /// 施加麻痹效果。
    /// </summary>
    /// <param name="durationSeconds">持续时间（秒）。</param>
    public void ApplyStun(float durationSeconds)
    {
        if (durationSeconds <= 0f)
        {
            return;
        }

        stunUntil = Mathf.Max(stunUntil, Time.time + durationSeconds);
    }

    /// <summary>
    /// 施加冰冻效果（同时延长麻痹至冰冻结束）。
    /// </summary>
    /// <param name="durationSeconds">持续时间（秒）。</param>
    public void ApplyFreeze(float durationSeconds)
    {
        if (durationSeconds <= 0f)
        {
            return;
        }

        freezeUntil = Mathf.Max(freezeUntil, Time.time + durationSeconds);
        stunUntil = Mathf.Max(stunUntil, freezeUntil);
    }

    /// <summary>
    /// 施加减速效果。
    /// </summary>
    /// <param name="durationSeconds">持续时间（秒）。</param>
    /// <param name="moveMultiplier">移速倍率（0.05~1）。</param>
    public void ApplySlow(float durationSeconds, float moveMultiplier)
    {
        if (durationSeconds <= 0f)
        {
            return;
        }

        slowUntil = Mathf.Max(slowUntil, Time.time + durationSeconds);
        slowMoveMultiplier = Mathf.Clamp(moveMultiplier, 0.05f, 1f);
    }

    /// <summary>清除所有控制类状态。</summary>
    public void ClearAll()
    {
        stunUntil = 0f;
        freezeUntil = 0f;
        slowUntil = 0f;
        slowMoveMultiplier = 1f;
    }

    /// <summary>禁用时自动清除状态（对象池回收安全）。</summary>
    private void OnDisable()
    {
        ClearAll();
    }
}
