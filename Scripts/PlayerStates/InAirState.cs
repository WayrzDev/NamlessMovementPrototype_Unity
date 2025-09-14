using UnityEngine;

public class InAirState : BaseState
{
    public override void Enter(MyCharacterController character)
    {
        Debug.Log("Entering InAir State");
    }

    public override void Exit(MyCharacterController character)
    {

    }

    public override void Update(MyCharacterController character, float deltaTime)
    {
        // Check transition conditions
        if (character.Motor.GroundingStatus.IsStableOnGround)
        {
            if (character.inputDirection != Vector2.zero)
                character.stateMachine.ChangeState(character.isRunning ? character.runstate : character.walkState, character);
            else
                character.stateMachine.ChangeState(character.idleState, character);
        }
        else if (character.velocity.y >= 0f)
        {
            character.stateMachine.ChangeState(character.jumpState, character);
        }
    }

    public override void HandleInput(MyCharacterController character)
    {
        if (Input.GetButtonDown("Jump"))
        {
            character.jump(0f, false);
            character.jumpBuffering();
        }
    }
}