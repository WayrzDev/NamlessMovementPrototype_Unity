using UnityEngine;

public class IdleState : BaseState
{
    public override void Enter(MyCharacterController character)
    {
        Debug.Log("Entering Idle State");
    }

    public override void Exit(MyCharacterController character)
    {

    }

    public override void Update(MyCharacterController character, float deltaTime)
    {
        // Check transition conditions
        if (character.inputDirection != Vector2.zero && character.Motor.GroundingStatus.IsStableOnGround)
        {
            character.stateMachine.ChangeState(character.walkState, character);
        }
        else if (!character.Motor.GroundingStatus.IsStableOnGround)
        {
            if (character.velocity.y >= 0f)
                character.stateMachine.ChangeState(character.jumpState, character);
            else
                character.stateMachine.ChangeState(character.inAirState, character);
        }
    }

    public override void HandleInput(MyCharacterController character)
    {
        if (Input.GetButtonDown("Jump"))
        {
            character.jump(0f, false);
            character.jumpBuffering();
        }

        // No método HandleInput, adicione após o run check:
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            character.stateMachine.ChangeState(character.crouchState, character);
        }
    }
}
