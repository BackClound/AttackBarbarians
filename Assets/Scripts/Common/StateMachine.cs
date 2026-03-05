using UnityEngine;

public class StateMachine
{
    [SerializeField] private EntityState currentState;

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
