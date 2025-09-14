using UnityEngine;

public class StateMachine
{
    private BaseState currentState;

    public BaseState CurrentState => currentState;
    public string CurrentStateName => currentState?.GetType().Name ?? "None";

    public void Initialize(BaseState startingState, MyCharacterController character)
    {
        currentState = startingState;
        currentState.Enter(character);
    }

    public void ChangeState(BaseState newState, MyCharacterController character)
    {
        if (currentState != null)
        {
            currentState.Exit(character);
        }

        currentState = newState;
        currentState.Enter(character);
    }

    public void Update(MyCharacterController character, float deltaTime)
    {
        currentState?.Update(character, deltaTime);
    }

    public void HandleInput(MyCharacterController character)
    {
        currentState?.HandleInput(character);
    }
}
