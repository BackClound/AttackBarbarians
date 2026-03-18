using UnityEngine;

public class EntityState
{
    protected StateMachine stateMachine;
    protected string animName;
    protected bool isAnimFinished;
    protected Animator anim;
    protected Rigidbody2D rb;

    public EntityState(StateMachine machine, string animName)
    {
        this.animName = animName;
        this.stateMachine = machine;
    }

    public virtual void OnEnter()
    {
        anim?.SetBool(animName, true);
        isAnimFinished = false;
    }

    public virtual void OnUpdate() { }

    public virtual void OnFixedUpdate() { }

    public virtual void OnExit()
    {
        anim.SetBool(animName, false);
    }

    public virtual void OnAnimFinished()
    {
        isAnimFinished = true;
    }

    public virtual void OnAnimAttackTrigger()
    {

    }

    public virtual void ApplyAnimSpeedMulti() { }
}
