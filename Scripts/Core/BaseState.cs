using UnityEngine;

public abstract class BaseState
{
    public abstract void Enter(MyCharacterController character);
    public abstract void Exit(MyCharacterController character);
    public abstract void Update(MyCharacterController character, float deltaTime);
    public abstract void HandleInput(MyCharacterController character);
}
