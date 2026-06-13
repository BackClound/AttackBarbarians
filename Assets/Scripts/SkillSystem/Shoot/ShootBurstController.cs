using UnityEngine;

/// <summary>
/// 射击连发与休整状态机：从 <see cref="SkillDataSO"/> / <see cref="SkillRuntime"/> 读取连发上限与休整冷却。
/// 用于 Legacy 连发射击流程，与 <see cref="SkillManager"/> 自动施法管线并行存在。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 Player 上，与 <see cref="SkillManager"/> 同物体。</para>
/// </remarks>
[DisallowMultipleComponent]
public class ShootBurstController : MonoBehaviour
{
    /// <summary>当前连发周期内已发射次数。</summary>
    private int burstShotsFired;
    /// <summary>连发是否已耗尽并进入休整。</summary>
    private bool burstExhausted;
    /// <summary>休整剩余秒数。</summary>
    private float recoveryTimer;

    /// <summary>
    /// 当前是否允许发射（已解锁、未休整、未达连发上限）。
    /// </summary>
    /// <param name="runtime">射击技能运行时。</param>
    /// <returns>可发射时返回 true。</returns>
    public bool CanShoot(SkillRuntime runtime) =>
        runtime != null && runtime.IsUnlocked && !burstExhausted && burstShotsFired < runtime.BaseData.MaxAttackCount;

    /// <summary>
    /// 记录一次发射；达连发上限时进入休整并读取冷却。
    /// </summary>
    /// <param name="runtime">射击技能运行时。</param>
    public void RecordShot(SkillRuntime runtime)
    {
        if (runtime == null)
        {
            return;
        }

        burstShotsFired++;
        if (burstShotsFired >= runtime.BaseData.MaxAttackCount)
        {
            burstExhausted = true;
            recoveryTimer = runtime.BaseData.Cooldown > 0f
                ? runtime.BaseData.Cooldown
                : runtime.Config != null ? runtime.Config.BaseCooldown : 4f;
            burstShotsFired = 0;
        }
    }

    /// <summary>
    /// 每帧递减休整计时，结束后恢复可射击状态。
    /// </summary>
    /// <param name="deltaTime">帧间隔秒数。</param>
    public void Tick(float deltaTime)
    {
        if (!burstExhausted)
        {
            return;
        }

        recoveryTimer -= deltaTime;
        if (recoveryTimer <= 0f)
        {
            burstExhausted = false;
            recoveryTimer = 0f;
        }
    }

    /// <summary>重置连发计数与休整状态。</summary>
    public void ResetBurst()
    {
        burstShotsFired = 0;
        burstExhausted = false;
        recoveryTimer = 0f;
    }
}
