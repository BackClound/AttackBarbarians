using UnityEngine;

/// <summary>
/// 射击连发与休整状态：从 <see cref="SkillDataSO"/> / <see cref="SkillRuntime"/> 读取参数。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 Player 上，与 <see cref="SkillManager"/> 同物体。</para>
/// </remarks>
[DisallowMultipleComponent]
public class ShootBurstController : MonoBehaviour
{
    // 连发射击次数
    private int burstShotsFired;
    // 连发射击已耗尽
    private bool burstExhausted;
    // 连发射击恢复时间
    private float recoveryTimer;

    // 是否可以射击
    public bool CanShoot(SkillRuntime runtime) =>
        runtime != null && runtime.IsUnlocked && !burstExhausted && burstShotsFired < runtime.BaseData.MaxAttackCount;

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

    public void ResetBurst()
    {
        burstShotsFired = 0;
        burstExhausted = false;
        recoveryTimer = 0f;
    }
}
