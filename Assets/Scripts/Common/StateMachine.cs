using UnityEngine;

public class StateMachine
{
    public EntityState currentState { get; private set; }

    public void InitialState(EntityState state)
    {
        if (currentState == null)
        {
            currentState = state;
            currentState.OnEnter();
        }
        else
        {
            Debug.Log("You should Change state to the New EntityState!!!");
        }
    }

    public void ChangeState(EntityState newState)
    {
        if (currentState == newState) return;
        currentState.OnExit();
        currentState = newState;
        currentState.OnEnter();
    }

    public void UpdateState()
    {
        currentState?.OnUpdate();
    }

    public void FixedUpdateState()
    {
        currentState?.OnFixedUpdate();
    }
}
