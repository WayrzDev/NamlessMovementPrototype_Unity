using UnityEngine;

public class RunState : BaseState
{
    public override void Enter(MyCharacterController character)
    {
        Debug.Log("Entering Run State");

        character.currentMoveSpeed = character.RunMoveSpeed;
        character.currentMoveAcceleration = character.RunAcceleration;
        character.currentMoveDeceleration = character.RunDeceleration;

        character.isRunning = true;
        character.walkOrRunState = "runstate";
    }

    public override void Exit(MyCharacterController character)
    {

    }

    public override void Update(MyCharacterController character, float deltaTime)
    {
        // Check transition conditions
        if (character.inputDirection == Vector2.zero)
        {
            if (character.velocity.magnitude < 1.0f)
                character.stateMachine.ChangeState(character.idleState, character);
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
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            character.stateMachine.ChangeState(character.walkState, character);
        }

        if (Input.GetButtonDown("Jump"))
        {
            character.jump(0f, false);
            character.jumpBuffering();
        }
    }
}