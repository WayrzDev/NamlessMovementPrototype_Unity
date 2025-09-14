using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Camera Variables")]
    [SerializeField] private float XAxisSensibility = 3f;
    [SerializeField] private float YAxisSensibility = 3f;
    [SerializeField] private float maxUpAngleView = -60f;
    [SerializeField] private float maxDownAngleView = 60f;

    [Header("Movement Changes Variables")]
    [SerializeField] private float crouchCameraDepth = -0.3f;
    [SerializeField] private float crouchCameraLerpSpeed = 8f;
    [SerializeField] private float slideCameraDepth = -0.5f;
    [SerializeField] private float slideCameraLerpSpeed = 12f;

    [Header("FOV Variables")]
    private float targetFOV;
    private float lastFOV;
    private float addonFOV;
    [SerializeField] private float baseFOV = 75f;
    [SerializeField] private float crouchFOV = 70f;
    [SerializeField] private float runFOV = 85f;
    [SerializeField] private float slideFOV = 95f;
    [SerializeField] private float dashFOV = 110f;
    [SerializeField] private float fovChangeSpeed = 8f;
    [SerializeField] private float fovChangeSpeedWhenDash = 15f;

    [Header("Bob Variables")]
    private float headBobValue;
    [SerializeField] private float bobFrequency = 2f;
    [SerializeField] private float bobAmplitude = 0.1f;

    [Header("Tilt Variables")]
    [SerializeField] private float camTiltRotationValue = 2f;
    [SerializeField] private float camTiltRotationSpeed = 8f;

    [Header("Camera Shake Variables")]
    private float shakeForce;
    [SerializeField] private float shakeDuration = 0.3f;
    private float shakeDurationRef;
    [SerializeField] private float shakeFade = 5f;
    private bool canCameraShake = false;

    [Header("References")]
    [SerializeField] private Camera camera;
    [SerializeField] private MyCharacterController playerChar;

    // Input and mouse variables
    private Vector2 mouseInput;
    private Vector2 playCharInputDir;
    private bool mouseLocked = true;

    // Position and rotation tracking
    private float baseYPosition = 1.5f;
    private float currentTilt = 0f;

    void Start()
    {
        SetMouseMode(true);

        if (camera == null)
            camera = GetComponentInChildren<Camera>();

        // Initialize position
        transform.localPosition = new Vector3(0f, baseYPosition, 0f);

        lastFOV = baseFOV;
        shakeDurationRef = shakeDuration;
    }

    void Update()
    {
        if (mouseLocked)
        {
            HandleInput();
        }

        Applies(Time.deltaTime);
        CameraBob(Time.deltaTime);
        CameraTilt(Time.deltaTime);
        FOVChange(Time.deltaTime);

        lastFOV = targetFOV;

        HandleMouseToggle();
    }

    private void HandleInput()
    {
        if (!mouseLocked) return;

        mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        transform.Rotate(Vector3.up * mouseInput.x * XAxisSensibility);
        camera.transform.Rotate(Vector3.right * -mouseInput.y * YAxisSensibility);

        Vector3 eulerAngles = camera.transform.localEulerAngles;
        eulerAngles.x = ClampAngle(eulerAngles.x, maxUpAngleView, maxDownAngleView);
        camera.transform.localEulerAngles = eulerAngles;
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle > 180f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }

    private void HandleMouseToggle()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetMouseMode(!mouseLocked);
        }
    }

    private void Applies(float delta)
    {
        if (playerChar == null) return;

        string currentState = playerChar.stateMachine.CurrentStateName.ToLower();
        Vector3 currentPos = transform.localPosition;
        float targetY = baseYPosition;
        float targetRotZ = 0f;

        switch (currentState)
        {
            case "idlestate":
                targetY = baseYPosition;
                targetRotZ = 0f;
                break;

            case "walkstate":
                targetY = baseYPosition;
                targetRotZ = 0f;
                break;

            case "runstate":
                targetY = baseYPosition;
                targetRotZ = 0f;
                break;

            case "crouchstate":
                targetY = baseYPosition + crouchCameraDepth;
                float crouchTilt = playCharInputDir.x != 0f ? Mathf.Deg2Rad * 6f * playCharInputDir.x : Mathf.Deg2Rad * 6f;
                targetRotZ = crouchTilt;
                break;

            default:
                if (currentState.Contains("slide"))
                {
                    targetY = baseYPosition + slideCameraDepth;
                    float slideTilt = playCharInputDir.x != 0f ? Mathf.Deg2Rad * 10f * playCharInputDir.x : Mathf.Deg2Rad * 10f;
                    targetRotZ = slideTilt;
                }
                else
                {
                    targetY = baseYPosition;
                    targetRotZ = 0f;
                }
                break;
        }

        // Apply smooth Y position
        currentPos.y = Mathf.Lerp(currentPos.y, targetY, crouchCameraLerpSpeed * delta);
        transform.localPosition = currentPos;

        // Apply smooth Z rotation (only for crouch/slide states)
        if (currentState == "crouchstate" || currentState.Contains("slide"))
        {
            Vector3 currentRotation = transform.localEulerAngles;
            currentRotation.z = Mathf.LerpAngle(currentRotation.z, targetRotZ * Mathf.Rad2Deg, slideCameraLerpSpeed * delta);
            transform.localEulerAngles = currentRotation;
        }
        else
        {
            // Reset Z rotation for other states, but don't override the tilt
            Vector3 currentRotation = transform.localEulerAngles;
            if (Mathf.Abs(currentTilt) < 0.1f) // Only reset if tilt is not active
            {
                currentRotation.z = Mathf.LerpAngle(currentRotation.z, 0f, slideCameraLerpSpeed * delta);
                transform.localEulerAngles = currentRotation;
            }
        }
    }

    private void CameraBob(float delta)
    {
        if (playerChar == null) return;

        string currentState = playerChar.stateMachine.CurrentStateName.ToLower();

        if (currentState != "slidestate" && !currentState.Contains("dash"))
        {
            bool isOnFloor = playerChar.Motor.GroundingStatus.IsStableOnGround;
            headBobValue += delta * playerChar.velocity.magnitude * (isOnFloor ? 1f : 0f);
            Vector3 bobOffset = HeadBob(headBobValue);

            Vector3 cameraPos = camera.transform.localPosition;
            cameraPos.x = bobOffset.x;
            cameraPos.y = bobOffset.y;
            camera.transform.localPosition = cameraPos;
        }
    }

    private Vector3 HeadBob(float time)
    {
        Vector3 pos = Vector3.zero;
        pos.y = Mathf.Sin(time * bobFrequency) * bobAmplitude;
        pos.x = Mathf.Cos(time * bobFrequency / 2f) * bobAmplitude;
        return pos;
    }

    private void CameraTilt(float delta)
    {
        if (playerChar == null) return;

        string currentState = playerChar.stateMachine.CurrentStateName.ToLower();

        if (playerChar.moveDirection != Vector3.zero &&
            currentState != "crouchstate" &&
            !currentState.Contains("slide"))
        {
            playCharInputDir = playerChar.inputDirection;

            float tiltAmount = -playCharInputDir.x * camTiltRotationValue;

            if (!playerChar.Motor.GroundingStatus.IsStableOnGround)
            {
                tiltAmount /= 1.6f;
            }

            currentTilt = Mathf.Lerp(currentTilt, tiltAmount, camTiltRotationSpeed * delta);
        }
        else
        {
            currentTilt = Mathf.Lerp(currentTilt, 0f, camTiltRotationSpeed * delta);
        }

        // Apply tilt to transform Z rotation
        Vector3 currentRotation = transform.localEulerAngles;
        currentRotation.z = currentTilt;
        transform.localEulerAngles = currentRotation;
    }

    private void FOVChange(float delta)
    {
        // FOV addon logic
        if (Mathf.Approximately(lastFOV, baseFOV)) addonFOV = 0f;
        if (Mathf.Approximately(lastFOV, runFOV)) addonFOV = 10f;
        if (Mathf.Approximately(lastFOV, slideFOV)) addonFOV = 30f;

        if (playerChar == null) return;

        string currentState = playerChar.stateMachine.CurrentStateName.ToLower();

        switch (currentState)
        {
            case "idlestate":
                targetFOV = baseFOV;
                break;
            case "crouchstate":
                targetFOV = crouchFOV;
                break;
            case "walkstate":
                targetFOV = baseFOV;
                break;
            case "runstate":
                targetFOV = runFOV;
                break;
            case "jumpstate":
                targetFOV = baseFOV + addonFOV;
                break;
            case "inairstate":
                targetFOV = baseFOV + addonFOV;
                break;
            default:
                if (currentState.Contains("slide"))
                    targetFOV = slideFOV;
                else if (currentState.Contains("dash"))
                    targetFOV = dashFOV;
                else
                    targetFOV = baseFOV;
                break;
        }

        float fovSpeed = currentState.Contains("dash") ? fovChangeSpeedWhenDash : fovChangeSpeed;
        camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, targetFOV, fovSpeed * delta);
    }

    private void SetMouseMode(bool captured)
    {
        mouseLocked = captured;
        if (captured)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}