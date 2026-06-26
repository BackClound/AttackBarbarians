using Spine;
using Spine.Unity;
using UnityEngine;
using SpineAnimationState = Spine.AnimationState;

/// <summary>
/// 将项目敌人状态机（Idle/Move/Attack/Dead）映射到 Spine 动画，并在攻击/死亡结束时
/// 回调 <see cref="Enemy.OnAnimatorAttackTrigger"/> 与 <see cref="Enemy.OnAniamtorFinished"/>。
/// </summary>
/// <remarks>
/// <para>挂在含 <see cref="SkeletonAnimation"/> 的视觉子物体上；无需 Unity Animator。</para>
/// </remarks>
[DisallowMultipleComponent]
[RequireComponent(typeof(SkeletonAnimation))]
public class SpineEnemyAnimationBridge : MonoBehaviour
{
    [Header("Spine Clips")]
    [SerializeField] private string idleAnimation = "idle";
    [SerializeField] private string moveAnimation = "idle";
    [SerializeField] private string attackAnimation = "attack";
    [SerializeField] private string deathAnimation = "die";

    [Header("Timing")]
    [SerializeField] [Range(0.05f, 0.95f)] private float attackHitNormalizedTime = 0.4f;

    private SkeletonAnimation skeletonAnimation;
    private Enemy enemy;
    private EntityState trackedState;
    private TrackEntry activeEntry;
    private bool attackHitRaised;

    private void Awake()
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();
    }

    private void OnEnable()
    {
        trackedState = null;
        attackHitRaised = false;
        activeEntry = null;
        PlayLoop(idleAnimation);
    }

    private void OnDisable()
    {
        if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
        {
            skeletonAnimation.AnimationState.Complete -= HandleTrackComplete;
        }
    }

    private void LateUpdate()
    {
        if (enemy == null)
        {
            enemy = GetComponentInParent<Enemy>();
        }

        if (enemy == null || skeletonAnimation == null || !skeletonAnimation.valid)
        {
            return;
        }

        EntityState current = enemy.stateMachine?.currentState;
        if (current == null)
        {
            return;
        }

        if (current != trackedState)
        {
            trackedState = current;
            attackHitRaised = false;
            EnterState(current);
        }

        TryRaiseAttackHit(current);
    }

    private void EnterState(EntityState state)
    {
        SpineAnimationState animState = skeletonAnimation.AnimationState;
        animState.Complete -= HandleTrackComplete;

        if (state is EnemyDeadState)
        {
            activeEntry = animState.SetAnimation(0, deathAnimation, false);
            animState.Complete += HandleTrackComplete;
        }
        else if (state is EnemyAttackState)
        {
            activeEntry = animState.SetAnimation(0, attackAnimation, false);
            animState.Complete += HandleTrackComplete;
        }
        else if (state is EnemyMoveState)
        {
            activeEntry = PlayLoop(moveAnimation);
        }
        else
        {
            activeEntry = PlayLoop(idleAnimation);
        }
    }

    private TrackEntry PlayLoop(string animationName)
    {
        if (string.IsNullOrEmpty(animationName))
        {
            return null;
        }

        return skeletonAnimation.AnimationState.SetAnimation(0, animationName, true);
    }

    private void TryRaiseAttackHit(EntityState state)
    {
        if (!(state is EnemyAttackState) || attackHitRaised || activeEntry == null)
        {
            return;
        }

        float duration = activeEntry.AnimationEnd - activeEntry.AnimationStart;
        if (duration <= 0.01f)
        {
            return;
        }

        float normalized = (activeEntry.TrackTime - activeEntry.AnimationStart) / duration;
        if (normalized < attackHitNormalizedTime)
        {
            return;
        }

        attackHitRaised = true;
        enemy.OnAnimatorAttackTrigger();
    }

    private void HandleTrackComplete(TrackEntry entry)
    {
        if (entry != activeEntry || enemy == null)
        {
            return;
        }

        enemy.OnAniamtorFinished();
    }
}
