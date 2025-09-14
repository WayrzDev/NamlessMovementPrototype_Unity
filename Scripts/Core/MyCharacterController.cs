using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;

public class MyCharacterController : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor Motor;

    [Header("Stable Movement")]
    public float currentMoveSpeed;
    public float currentMoveAcceleration;
    public float currentMoveDeceleration;
    public float desiredMoveSpeed;
    public float maxSpeed = 80;

    [Header("Walk Variables")]
    public float WalkMoveSpeed;
    public float WalkDeceleration;
    public float WalkAcceleration;

    [Header("Run Variables")]
    public float RunMoveSpeed;
    public float RunDeceleration;
    public float RunAcceleration;

    [Header("Crouch Variables")]
    public float CrouchMoveSpeed;
    public float CrouchDeceleration;
    public float CrouchAcceleration;

    [Header("Slide Variables WIP")]
    [Header("Dash Variables WIP")]

    [Header("Movement Variables")]
    public float hitGroundCooldown = 0.2f;
    private float hitGroundCooldownRef;
    public AnimationCurve desiredMoveSpeedCurve;
    public AnimationCurve inAirMoveSpeedCurve;

    [Header("Jump Variables")]
    public float jumpHeight = 2f;
    public float jumpTimeToPeak = 0.5f;
    public float jumpTimeToFall = 0.5f;
    public float jumpCooldown = 0.1f;
    public int nbJumpsInAirAllowed = 1;
    private int nbJumpsInAirAllowedRef;
    public bool canCoyoteJump = true;
    public float coyoteJumpCooldown = 0.1f;
    private float coyoteJumpCooldownRef;
    private float coyoteJumpTimer;
    private bool coyoteJumpOn = false;
    private bool jumpBuffOn = false;
    private float jumpCooldownTimer;
    private float jumpCooldownRef;

    [Header("Crouch System")]
    public float ceilingCheckDistance = 1.0f;
    public bool isCrouching = false;

    [HideInInspector] public float jumpVelocity;
    [HideInInspector] public float jumpGravity;
    [HideInInspector] public float fallGravity;
    [HideInInspector] public float hitGroundCooldownTimer;
    [HideInInspector] public bool wasOnFloor = false;
    public bool isRunning = false;
    public string walkOrRunState = "walkstate";

    [Header("Misc")]
    public Vector3 velocity;

    public Vector3 moveDirection;
    public Vector2 inputDirection;
    public Transform cameraHolder;

    private Vector3 lastFramePosition;

    // State Machine
    [Header("State Machine")]
    public StateMachine stateMachine;
    public IdleState idleState = new IdleState();
    public WalkState walkState = new WalkState();
    public JumpState jumpState = new JumpState();
    public RunState runstate = new RunState();
    public CrouchState crouchState = new CrouchState();
    public InAirState inAirState = new InAirState();

    private void Start()
    {
        // Assign to motor
        Motor.CharacterController = this;
        desiredMoveSpeed = currentMoveSpeed;
        hitGroundCooldownRef = hitGroundCooldown;

        currentMoveAcceleration = WalkAcceleration;
        currentMoveSpeed = WalkMoveSpeed;
        currentMoveDeceleration = WalkDeceleration;

        jumpCooldownRef = jumpCooldown;
        nbJumpsInAirAllowedRef = nbJumpsInAirAllowed;
        coyoteJumpCooldownRef = coyoteJumpCooldown;
        coyoteJumpTimer = coyoteJumpCooldownRef;
        jumpCooldownTimer = 0f;

        // Initialize jump calculations (exact Godot calculation)
        jumpGravity = -2f * jumpHeight / (jumpTimeToPeak * jumpTimeToPeak);
        fallGravity = -2f * jumpHeight / (jumpTimeToFall * jumpTimeToFall);
        jumpVelocity = (2f * jumpHeight) / jumpTimeToPeak;

        hitGroundCooldownTimer = hitGroundCooldownRef;

        // Initialize State Machine
        stateMachine = new StateMachine();
        stateMachine.Initialize(idleState, this);
    }

    public void Update()
    {
        inputManagement();
        stateMachine.Update(this, Time.deltaTime);
    }

    public bool IsCeilingBlocked()
    {
        // Usar as dimensões originais do capsule para verificar se cabe
        float originalHeight = 2f; // Configure conforme seu personagem
        float currentHeight = Motor.Capsule.height;

        // Se já estamos com altura total, não há obstrução
        if (Mathf.Approximately(currentHeight, originalHeight))
            return false;

        // Calcular onde ficaria o topo se voltássemos ao tamanho normal
        float heightDifference = originalHeight - currentHeight;
        Vector3 checkPosition = transform.position + Vector3.up * (heightDifference * 0.5f);

        // Fazer um SphereCast para verificar obstrução
        return Physics.SphereCast(
            checkPosition,
            Motor.Capsule.radius * 0.9f,
            Vector3.up,
            out RaycastHit hit,
            heightDifference * 0.5f + 0.1f
        );
    }


    /// <summary>
    /// This is called every frame by MyPlayer in order to tell the character what its inputs are
    /// </summary>
    public void inputManagement()
    {
        stateMachine.HandleInput(this);
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is called before the character begins its movement update
    /// </summary>
    public void BeforeCharacterUpdate(float deltaTime)
    {
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is where you tell your character what its rotation should be right now. 
    /// This is the ONLY place where you should set the character's rotation
    /// </summary>
    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {

    }

    public void Applies(float deltaTime)
    {
        if (!Motor.GroundingStatus.IsStableOnGround)
        {
            if (velocity.y >= 0f)
            {
                velocity.y += jumpGravity * deltaTime;
            }
            else
            {
                velocity.y += fallGravity * deltaTime;
            }

            if (hitGroundCooldown != hitGroundCooldownRef)
            {
                hitGroundCooldown = hitGroundCooldownRef;
            }

            if (coyoteJumpTimer > 0.0f)
            {
                coyoteJumpTimer -= deltaTime;
            }
        }

        if (Motor.GroundingStatus.IsStableOnGround)
        {
            // Jump buffering check
            if (jumpBuffOn)
            {
                jumpBuffOn = false;
                jump(0.0f, false);
            }

            if (hitGroundCooldown >= 0)
            {
                hitGroundCooldown -= deltaTime;
            }

            if (nbJumpsInAirAllowed != nbJumpsInAirAllowedRef)
            {
                nbJumpsInAirAllowed = nbJumpsInAirAllowedRef;
            }

            if (coyoteJumpTimer != coyoteJumpCooldownRef)
            {
                coyoteJumpTimer = coyoteJumpCooldownRef;
            }
        }

        // Timer management (aplicado sempre)
        if (jumpCooldownTimer > 0.0f)
        {
            jumpCooldownTimer -= deltaTime;
        }
    }

    public void jump(float jumpBoostValue, bool isJumpBoost)
    {
        bool canJump = false; // jump condition

        // On wall jump (se você tiver wall jump)
        // if (is_on_wall() && canWallRun) { ... }

        // In air jump
        if (!Motor.GroundingStatus.IsStableOnGround)
        {
            if (jumpCooldownTimer <= 0f)
            {
                // Determine if the character are in the conditions for enable coyote jump
                if (wasOnFloor && coyoteJumpTimer > 0.0f && lastFramePosition.y > transform.position.y)
                {
                    coyoteJumpOn = true;
                }

                // If the character jump from a jumppad, the jump isn't taken into account
                if ((nbJumpsInAirAllowed > 0) || (nbJumpsInAirAllowed <= 0 && isJumpBoost) || (coyoteJumpOn))
                {
                    if (!isJumpBoost && !coyoteJumpOn)
                    {
                        nbJumpsInAirAllowed -= 1;
                    }
                    jumpCooldownTimer = jumpCooldownRef;
                    coyoteJumpOn = false;
                    canJump = true;
                }
            }
        }
        else // On floor jump
        {
            jumpCooldownTimer = jumpCooldownRef;
            canJump = true;
        }

        // Apply jump
        if (canJump)
        {
            if (isJumpBoost)
            {
                nbJumpsInAirAllowed = nbJumpsInAirAllowedRef;
            }

            Debug.Log($"Jumping! velocity.y: {jumpVelocity + jumpBoostValue}");
            velocity.y = jumpVelocity + jumpBoostValue;
            Motor.ForceUnground(0.1f);
            canJump = false;
        }
    }

    public void jumpBuffering()
    {
        // If the character is falling, and the floor check raycast is colliding
        if (IsFloorColliding() && lastFramePosition.y > transform.position.y &&
            nbJumpsInAirAllowed <= 0 && jumpCooldownTimer <= 0.0f)
        {
            jumpBuffOn = true;
        }
    }

    // Função auxiliar para detectar chão (equivalente ao floorCheck.is_colliding() do Godot)
    private bool IsFloorColliding()
    {
        return Physics.Raycast(transform.position, Vector3.down, 2.0f);
    }


    public void Move(float deltaTime)
    {
        Vector3 targetMovementVelocity = Vector3.zero;

        if (Motor.GroundingStatus.IsStableOnGround)
        {
            if (moveDirection != Vector3.zero)
            {
                velocity = Motor.GetDirectionTangentToSurface(velocity, Motor.GroundingStatus.GroundNormal) * velocity.magnitude;

                // Calculate target velocity
                Vector3 inputRight = Vector3.Cross(moveDirection, Motor.CharacterUp);
                Vector3 reorientedInput = Vector3.Cross(Motor.GroundingStatus.GroundNormal, inputRight).normalized * moveDirection.magnitude;
                targetMovementVelocity = reorientedInput * currentMoveSpeed;

                if (hitGroundCooldown <= 0) desiredMoveSpeed = velocity.magnitude;

                // Smooth movement Velocity
                velocity = Vector3.Lerp(velocity, targetMovementVelocity, 1 - Mathf.Exp(-currentMoveAcceleration * deltaTime));
            }
            else
            {
                velocity = Vector3.Lerp(velocity, Vector3.zero, 1 - Mathf.Exp(-currentMoveDeceleration * deltaTime));
                desiredMoveSpeed = velocity.magnitude;
            }
        }
        if (!Motor.GroundingStatus.IsStableOnGround)
        {
            if (moveDirection != Vector3.zero)
            {
                if (desiredMoveSpeed < maxSpeed)
                {
                    desiredMoveSpeed += 1.5f * deltaTime;
                }


                float contrdDesMoveSpeed = desiredMoveSpeedCurve.Evaluate(desiredMoveSpeed / 100f);
                float contrdInAirMoveSpeed = inAirMoveSpeedCurve.Evaluate(desiredMoveSpeed);

                velocity.x = Mathf.Lerp(velocity.x, moveDirection.x * contrdDesMoveSpeed, contrdInAirMoveSpeed * deltaTime);
                velocity.z = Mathf.Lerp(velocity.z, moveDirection.z * contrdDesMoveSpeed, contrdInAirMoveSpeed * deltaTime);
            }
            else
            {
                desiredMoveSpeed = velocity.magnitude;
            }
        }

        if (desiredMoveSpeed >= maxSpeed)
        {
            desiredMoveSpeed = maxSpeed;
        }

        lastFramePosition = transform.position;
        wasOnFloor = !Motor.GroundingStatus.IsStableOnGround;
    }
    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is where you tell your character what its velocity should be right now. 
    /// This is the ONLY place where you can set the character's velocity
    /// </summary>
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {

        Applies(deltaTime);
        Move(deltaTime);
        currentVelocity = velocity;
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is called after the character has finished its movement update
    /// </summary>
    public void AfterCharacterUpdate(float deltaTime)
    {
        velocity = Motor.BaseVelocity;

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        inputDirection = new Vector2(moveX, moveZ);

        moveDirection = CalculateCameraRelativeDirection();
    }

    private Vector3 CalculateCameraRelativeDirection()
    {
        if (cameraHolder == null || inputDirection == Vector2.zero)
            return Vector3.zero;

        Vector3 cameraForward = cameraHolder.forward;
        Vector3 cameraRight = cameraHolder.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        if (cameraForward.magnitude < 0.1f)
        {
            float yRotation = cameraHolder.eulerAngles.y * Mathf.Deg2Rad;
            cameraForward = new Vector3(Mathf.Sin(yRotation), 0f, Mathf.Cos(yRotation));
            cameraRight = new Vector3(Mathf.Cos(yRotation), 0f, -Mathf.Sin(yRotation));
        }
        else
        {
            cameraForward.Normalize();
            cameraRight.Normalize();
        }

        Vector3 direction = (cameraForward * inputDirection.y) + (cameraRight * inputDirection.x);
        return direction.normalized;
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 30), $"Current State: {stateMachine.CurrentStateName}");
        GUI.Label(new Rect(10, 30, 300, 30), $"Move speed: {currentMoveSpeed}");
        GUI.Label(new Rect(10, 50, 300, 30), $"Desired move speed: {desiredMoveSpeed}");
        GUI.Label(new Rect(10, 70, 300, 30), $"Velocity: {velocity.magnitude:F1}");
        GUI.Label(new Rect(10, 90, 300, 30), $"Number jumps allowed in air: {nbJumpsInAirAllowed}");
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        return true;
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {

    }

    public void AddVelocity(Vector3 velocity)
    {
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void PostGroundingUpdate(float deltaTime)
    {
        // Handle landing and leaving ground
        if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
        {
            OnLanded();
        }
        else if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
        {
            OnLeaveStableGround();
        }
    }

    protected void OnLanded()
    {
        Debug.Log("Landed");
    }

    protected void OnLeaveStableGround()
    {
        Debug.Log("Left ground");
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }
}