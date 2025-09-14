using UnityEngine;

public class CrouchState : BaseState
{
    public override void Enter(MyCharacterController character)
    {
        Debug.Log("Entering Crouch State");

        // Set crouch movement properties
        character.currentMoveSpeed = character.CrouchMoveSpeed;
        character.currentMoveAcceleration = character.CrouchAcceleration;
        character.currentMoveDeceleration = character.CrouchDeceleration;

        character.Motor.SetCapsuleDimensions(.5f, 1.01f, 0.5f);
        character.isCrouching = true;
    }

    public override void Exit(MyCharacterController character)
    {
        character.Motor.SetCapsuleDimensions(.5f, 2f, 1f);
        character.isCrouching = false;
    }

    public override void Update(MyCharacterController character, float deltaTime)
    {
        // Check transition conditions
        if (character.inputDirection == Vector2.zero)
        {
            if (character.velocity.magnitude < 1.0f)
            {
                // Stay in crouch when not moving (crouch idle)
            }
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
        // Exit crouch if crouch button is pressed again and ceiling is clear
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (!character.IsCeilingBlocked())
            {
                character.stateMachine.ChangeState(character.walkState, character);
            }
        }

        // Can't run while crouching, but if run is pressed and ceiling is clear, go to walk first
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (!character.IsCeilingBlocked())
            {
                character.stateMachine.ChangeState(character.walkState, character);
            }
        }

        if (Input.GetButtonDown("Jump"))
        {
            character.jump(0f, false);
            character.jumpBuffering();
        }
    }
}