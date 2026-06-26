using UnityEngine;

/// <summary>
/// 基于 Unity Animator 布尔参数的默认动画驱动。
/// </summary>
public sealed class AnimatorEntityAnimationDriver : IEntityAnimationDriver
{
    private readonly Animator animator;
    private string pendingPulseParam;

    public AnimatorEntityAnimationDriver(Animator animator)
    {
        this.animator = animator;
    }

    public void OnStateEnter(EntityState state, string animParam)
    {
        if (animator == null || string.IsNullOrEmpty(animParam))
        {
            return;
        }

        animator.SetBool(animParam, true);
    }

    public void OnStateExit(EntityState state, string animParam)
    {
        if (animator == null || string.IsNullOrEmpty(animParam))
        {
            return;
        }

        animator.SetBool(animParam, false);
    }

    public void SetFloat(string paramName, float value)
    {
        if (animator == null || string.IsNullOrEmpty(paramName))
        {
            return;
        }

        animator.SetFloat(paramName, value);
    }

    public void PlayPulse(string paramName)
    {
        if (animator == null || string.IsNullOrEmpty(paramName))
        {
            return;
        }

        animator.SetBool(paramName, true);
        pendingPulseParam = paramName;
    }

    public void OnAnimationEventFinished()
    {
        if (animator == null || string.IsNullOrEmpty(pendingPulseParam))
        {
            return;
        }

        animator.SetBool(pendingPulseParam, false);
        pendingPulseParam = null;
    }

    public void ResetDriver()
    {
        pendingPulseParam = null;
        if (animator == null)
        {
            return;
        }

        animator.Rebind();
        animator.Update(0f);
    }
}
