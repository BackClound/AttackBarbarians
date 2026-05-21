using UnityEngine;

/// <summary>
/// 敌人控制类状态：麻痹、冰冻、减速；由技能系统写入，<see cref="EnemyState"/> 读取移速倍率。
/// </summary>
[DisallowMultipleComponent]
public class EnemyStatusController : MonoBehaviour
{
    private float stunUntil;
    private float freezeUntil;
    private float slowUntil;
    private float slowMoveMultiplier = 1f;

    public bool IsStunned => Time.time < stunUntil;
    public bool IsFrozen => Time.time < freezeUntil;
    public bool IsMovementBlocked => IsStunned || IsFrozen;

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

    public void ApplyStun(float durationSeconds)
    {
        if (durationSeconds <= 0f)
        {
            return;
        }

        stunUntil = Mathf.Max(stunUntil, Time.time + durationSeconds);
    }

    public void ApplyFreeze(float durationSeconds)
    {
        if (durationSeconds <= 0f)
        {
            return;
        }

        freezeUntil = Mathf.Max(freezeUntil, Time.time + durationSeconds);
        stunUntil = Mathf.Max(stunUntil, freezeUntil);
    }

    public void ApplySlow(float durationSeconds, float moveMultiplier)
    {
        if (durationSeconds <= 0f)
        {
            return;
        }

        slowUntil = Mathf.Max(slowUntil, Time.time + durationSeconds);
        slowMoveMultiplier = Mathf.Clamp(moveMultiplier, 0.05f, 1f);
    }

    public void ClearAll()
    {
        stunUntil = 0f;
        freezeUntil = 0f;
        slowUntil = 0f;
        slowMoveMultiplier = 1f;
    }

    private void OnDisable()
    {
        ClearAll();
    }
}
