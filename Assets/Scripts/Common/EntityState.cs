using UnityEngine;

public class EntityState
{
    protected StateMachine stateMachine;
    protected string animName;

    protected bool isAnimEnd;

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
        isAnimEnd = false;
    }

    public virtual void OnUpdate() { }

    public virtual void OnFixedUpdate() { }

    public virtual void OnExit()
    {
        anim.SetBool(animName, false);
    }

    public void OnAnimEnd()
    {
        isAnimEnd = true;
    }

    public virtual void ApplyAnimSpeedMulti() { }
}
