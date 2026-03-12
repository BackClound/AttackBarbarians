using UnityEngine;

public class PlayerAnimatorTrigger : EntityAnimatorTrigger
{
    private Player player;

    private void Awake()
    {
        if (player == null)
            player = GetComponentInParent<Player>();
    }
    public override void OnAnimationFinished()
    {
        player?.OnAniamtorFinished();
    }

    public override void OnAnimEventTrigger()
    {
        player?.OnAnimatorEventTrigger();
    }
}
