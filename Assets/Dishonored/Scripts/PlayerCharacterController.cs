using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class PlayerCharacterController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the main camera used for the player")]
    private Camera m_PlayerCamera;

    [Header("General")]
    [Tooltip("Force applied downward when in the air"), SerializeField]
    private float GravityDownForce = 20f;
    [Tooltip("Physic layers checked to consider the player grounded"), SerializeField]
    private LayerMask GroundCheckLayers = -1;
    [Tooltip("distance from the bottom of the character controller capsule to test for grounded"), SerializeField]
    private float GroundCheckDistance = 0.05f;

    [Header("Movement")]
    [Tooltip("Max movement speed when grounded (when not sprinting)"), SerializeField]
    private float MaxSpeedOnGround = 10f;
    [Tooltip("Sharpness for the movement when grounded, a low value will make the player accelerate and decelerate slowly, a high value will do the opposite"), SerializeField]
    private float MovementSharpnessOnGround = 15;
    [Tooltip("Max movement speed when crouching"), SerializeField]
    [Range(0,1)]
    private float MaxSpeedCrouchedRatio = 0.5f;
    [Tooltip("Max movement speed when not grounded"), SerializeField]
    private float MaxSpeedInAir = 10f;
    [Tooltip("Acceleration speed when in the air")]
    private float AccelerationSpeedInAir = 25f;
    [Tooltip("Multiplicator for the sprint speed (based on grounded speed)"), SerializeField]
    private float SprintSpeedModifier = 2f;

    [Header("Rotation")]
    [Tooltip("Rotation speed for moving the camera"), SerializeField]
    private float RotationSpeed = 200f;

    [Header("Jump")]
    [Tooltip("Force applied upward when jumping"), SerializeField]
    private float JumpForce = 9f;

    [Header("Stance")]
    [Tooltip("Ratio (0-1) of the character height where the camera will be at"), SerializeField]
    private float CameraHeightRatio = 0.9f;
    [Tooltip("Height of character when standing"), SerializeField]
    private float HeightStanding = 1.8f;
    [Tooltip("Height of character when crouching"), SerializeField]
    private float HeightCrouching = 0.9f;
    [Tooltip("Speed of crouching transitions"), SerializeField]
    private float CrouchingSharpness = 10f;
    [Tooltip("Character Controller radius"), Range(0.1f, 0.4f), SerializeField]
    private float PlayerCapsuleRadius = 0.35f;

    [Header("Camera Effects")]
    [SerializeField]
    private float TiltCameraZ;
    [SerializeField]
    private float TiltCameraZDamp;
    [SerializeField]
    private float RunningFov;
    [SerializeField]
    private float FovIncreaseDamp;
    [SerializeField]
    private float FovDecreaseDamp;
    [SerializeField]
    private float BobAnimDuration;
    [SerializeField]
    private AnimationCurve BobAnimationCurve;

    private Vector3 m_PlayerInput;

    private UnityAction<bool> onStanceChanged;

    public Vector3 characterVelocity { get; set; }
    public bool isGrounded { get; private set; }
    public bool isLanded { get; private set; }
    public bool hasJumpedThisFrame { get; private set; }
    public bool isCrouching { get; private set; }
    public bool isSprinting { get; private set; }
    public CharacterController CharacterController { get => m_Controller; }
    public Vector3 PlayerInput { get => m_PlayerInput; }
    public float PlayerHeightStanding { get => HeightStanding; }
    public float PlayerHeightCrouching { get => HeightCrouching; }

    private CharacterController m_Controller;

    private Vector3 m_GroundNormal;
    private float m_LastTimeJumped;
    private float m_CameraVerticalAngle;
    private float m_TargetCharacterHeight;
    private float m_CurrentTilt;
    private float m_DefaultCameraFov;
    private float m_BobAnimationState;

    const float k_JumpGroundingPreventionTime = 0.2f;
    const float k_GroundCheckDistanceInAir = 0.07f;

    private void Awake()
    {
        m_PlayerCamera = GetComponentInChildren<Camera>();
        m_Controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        m_Controller.enableOverlapRecovery = true;
        m_Controller.radius = PlayerCapsuleRadius;

        m_DefaultCameraFov = m_PlayerCamera.fieldOfView;

        // force the crouch state to false when starting
        SetCrouchingState(false, true);
        UpdateCharacterHeight(true);

        SetBobAnimationCurve(m_PlayerCamera.transform.localPosition.y);
    }

    private void Update()
    {
        hasJumpedThisFrame = false;

        bool wasGrounded = isGrounded;
        GroundCheck();

        // landing
        isLanded = isGrounded && !wasGrounded;

        if (isLanded && !isCrouching)
        {
            m_BobAnimationState = 0;
        }

        UpdateCameraBobAnimation();

        // crouching
        if (PlayerInputHandler.Instance.GetCrouchInputDown() && !TimeManager.Instance.TimeStopped)
        {
            m_BobAnimationState = 1;
            SetCrouchingState(!isCrouching, false);
        }

        UpdateCharacterHeight(false);

        HandleCharacterMovement();
    }

    private void UpdateCameraBobAnimation()
    {
        if(m_BobAnimationState < BobAnimDuration)
        {
            m_BobAnimationState += Time.unscaledDeltaTime;
            float t = m_BobAnimationState / BobAnimDuration;
            m_PlayerCamera.transform.localPosition = Vector3.up * BobAnimationCurve.Evaluate(t);
        }
    }

    private void SetBobAnimationCurve(float defaultY)
    {
        BobAnimationCurve = new AnimationCurve();
        BobAnimationCurve.AddKey(new Keyframe(0, defaultY));
        BobAnimationCurve.AddKey(new Keyframe(0.085f, defaultY * 0.775f));
        BobAnimationCurve.AddKey(new Keyframe(0.45f, defaultY * 0.999f));
        BobAnimationCurve.AddKey(new Keyframe(0.725f, defaultY * 0.990f));
        BobAnimationCurve.AddKey(new Keyframe(1.0f, defaultY));
    }

    void GroundCheck()
    {
        // Make sure that the ground check distance while already in air is very small, to prevent suddenly snapping to ground
        float chosenGroundCheckDistance = isGrounded ? (m_Controller.skinWidth + GroundCheckDistance) : k_GroundCheckDistanceInAir;

        // reset values before the ground check
        isGrounded = false;
        m_GroundNormal = Vector3.up;

        // only try to detect ground if it's been a short amount of time since last jump; otherwise we may snap to the ground instantly after we try jumping
        if (Time.time >= m_LastTimeJumped + k_JumpGroundingPreventionTime)
        {
            // if we're grounded, collect info about the ground normal with a downward capsule cast representing our character capsule
            if (Physics.CapsuleCast(
                GetCapsuleBottomHemisphere(), 
                GetCapsuleTopHemisphere(m_Controller.height), 
                m_Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance,
                GroundCheckLayers, QueryTriggerInteraction.Ignore))
            {
                // storing the upward direction for the surface found
                m_GroundNormal = hit.normal;
                
                // Only consider this a valid ground hit if the ground normal goes in the same direction as the character up
                // and if the slope angle is lower than the character controller's limit
                if (Vector3.Dot(hit.normal, transform.up) > 0f &&
                    IsNormalUnderSlopeLimit(m_GroundNormal))
                {
                    isGrounded = true;

                    // handle snapping to the ground
                    if (hit.distance > m_Controller.skinWidth && m_Controller.enabled)
                    {
                        m_Controller.Move(Vector3.down * hit.distance);
                    }
                }
            }
        }
    }

    void HandleCharacterMovement()
    {
        m_PlayerInput = PlayerInputHandler.Instance.GetMoveInput();

        // horizontal character rotation
        {
            // rotate the transform with the input speed around its local Y axis
            transform.Rotate(new Vector3(0f, (PlayerInputHandler.Instance.GetLookInputsHorizontal() * RotationSpeed), 0f), Space.Self);
        }

        // vertical camera rotation
        {
            float tiltTarget = TiltCameraZ * PlayerInput.x;

            if (m_PlayerInput != Vector3.zero)
            {
                m_CurrentTilt = Mathf.Lerp(m_CurrentTilt, tiltTarget, 1 - Mathf.Exp(-TiltCameraZDamp * Time.unscaledDeltaTime));
            }
            else
            {
                m_CurrentTilt = Mathf.Lerp(m_CurrentTilt, 0, 1 - Mathf.Exp(-TiltCameraZDamp * Time.unscaledDeltaTime));
            }

            // add vertical inputs to the camera's vertical angle
            m_CameraVerticalAngle += PlayerInputHandler.Instance.GetLookInputsVertical() * RotationSpeed;

            // limit the camera's vertical angle to min/max
            m_CameraVerticalAngle = Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);

            // apply the vertical angle as a local rotation to the camera transform along its right axis (makes it pivot up and down)
            m_PlayerCamera.transform.localEulerAngles = new Vector3(m_CameraVerticalAngle, 0, m_CurrentTilt);
        }

        // character movement handling
        isSprinting = PlayerInputHandler.Instance.GetSprintInputHeld();
        {
            if (isSprinting && PlayerInput.sqrMagnitude > 0f)
            {
                m_PlayerCamera.fieldOfView = Mathf.Lerp(m_PlayerCamera.fieldOfView, RunningFov, 1 - Mathf.Exp(-FovIncreaseDamp * Time.unscaledDeltaTime));
                isSprinting = SetCrouchingState(false, false);
            }
            else if(m_PlayerCamera.fieldOfView > m_DefaultCameraFov)
            {
                m_PlayerCamera.fieldOfView = Mathf.Lerp(m_PlayerCamera.fieldOfView, m_DefaultCameraFov, 1 - Mathf.Exp(-FovDecreaseDamp * Time.unscaledDeltaTime));
            }

            float speedModifier = isSprinting ? SprintSpeedModifier : 1f;

            // converts move input to a worldspace vector based on our character's transform orientation
            Vector3 worldspaceMoveInput = transform.TransformVector(m_PlayerInput);

            // handle grounded movement
            if (isGrounded)
            {
                // calculate the desired velocity from inputs, max speed, and current slope
                Vector3 targetVelocity = worldspaceMoveInput * MaxSpeedOnGround * speedModifier;
                // reduce speed if crouching by crouch speed ratio
                if (isCrouching)
                    targetVelocity *= MaxSpeedCrouchedRatio;
                targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, m_GroundNormal) * targetVelocity.magnitude;

                // smoothly interpolate between our current velocity and the target velocity based on acceleration speed
                characterVelocity = Vector3.Lerp(characterVelocity, targetVelocity, 1 - Mathf.Exp(-MovementSharpnessOnGround * Time.deltaTime));

                // jumping
                if (isGrounded && PlayerInputHandler.Instance.GetJumpInputDown())
                {
                    // force the crouch state to false
                    if (SetCrouchingState(false, false))
                    {
                        // start by canceling out the vertical component of our velocity
                        characterVelocity = new Vector3(characterVelocity.x, 0f, characterVelocity.z);

                        // then, add the jumpSpeed value upwards
                        characterVelocity += Vector3.up * JumpForce;

                        // remember last time we jumped because we need to prevent snapping to ground for a short time
                        m_LastTimeJumped = Time.time;
                        hasJumpedThisFrame = true;

                        // Force grounding to false
                        isGrounded = false;
                        m_GroundNormal = Vector3.up;
                    }
                }
            }
            // handle air movement
            else
            {
                // add air acceleration
                characterVelocity += worldspaceMoveInput * AccelerationSpeedInAir * Time.deltaTime;

                // limit air speed to a maximum, but only horizontally
                float verticalVelocity = characterVelocity.y;
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(characterVelocity, Vector3.up);
                horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, MaxSpeedInAir * speedModifier);
                characterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

                // apply the gravity to the velocity
                characterVelocity += Vector3.down * GravityDownForce * Time.deltaTime;
            }
        }

        // apply the final calculated velocity value as a character movement
        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(m_Controller.height);
        
        if(m_Controller.enabled)
            m_Controller.Move(characterVelocity * Time.deltaTime);

        if (Physics.CapsuleCast(
            capsuleBottomBeforeMove, 
            capsuleTopBeforeMove, 
            m_Controller.radius, 
            characterVelocity.normalized, 
            out RaycastHit hit,
            characterVelocity.magnitude * Time.deltaTime, -1, QueryTriggerInteraction.Ignore))
        {
            characterVelocity = Vector3.ProjectOnPlane(characterVelocity, hit.normal);
        }
    }

    // Returns true if the slope angle represented by the given normal is under the slope angle limit of the character controller
    bool IsNormalUnderSlopeLimit(Vector3 normal)
    {
        return Vector3.Angle(transform.up, normal) <= m_Controller.slopeLimit;
    }

    // Gets the center point of the bottom hemisphere of the character controller capsule    
    Vector3 GetCapsuleBottomHemisphere()
    {
        return transform.position + (transform.up * m_Controller.radius);
    }

    // Gets the center point of the top hemisphere of the character controller capsule    
    Vector3 GetCapsuleTopHemisphere(float atHeight)
    {
        return transform.position + (transform.up * (atHeight - m_Controller.radius));
    }

    // Gets a reoriented direction that is tangent to a given slope
    public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
    {
        Vector3 directionRight = Vector3.Cross(direction, transform.up);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }

    void UpdateCharacterHeight(bool force)
    {
        // Update height instantly
        if (force)
        {
            m_Controller.height = m_TargetCharacterHeight;
            m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
            m_PlayerCamera.transform.localPosition = Vector3.up * m_TargetCharacterHeight * CameraHeightRatio;
        }
        // Update smooth height
        else if (m_Controller.height != m_TargetCharacterHeight)
        {
            // resize the capsule and adjust camera position
            m_Controller.height = Mathf.Lerp(m_Controller.height, m_TargetCharacterHeight, 
                1 - Mathf.Exp(-CrouchingSharpness * Time.unscaledDeltaTime));
            m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
            m_PlayerCamera.transform.localPosition =
                Vector3.Lerp(
                    m_PlayerCamera.transform.localPosition, 
                    Vector3.up * m_TargetCharacterHeight * CameraHeightRatio,
                    1 - Mathf.Exp(-CrouchingSharpness * Time.unscaledDeltaTime));
        }
    }

    // returns false if there was an obstruction
    bool SetCrouchingState(bool crouched, bool ignoreObstructions)
    {
        // set appropriate heights
        if (crouched)
        {
            StartCoroutine(PPManager.Instance.VignetteSmoothness(0, 0.2f, 0.1f));
            m_TargetCharacterHeight = HeightCrouching;
        }
        else
        {
            if (!ignoreObstructions)
            {
                if(IsObstructed(HeightStanding))
                {
                    return false;
                }
            }

            PPManager.Instance.ResetVignette();
            m_TargetCharacterHeight = HeightStanding;
        }

        if (onStanceChanged != null)
        {
            onStanceChanged.Invoke(crouched);
        }

        isCrouching = crouched;
        return true;
    }

    public bool IsObstructed(float height)
    {
        Collider[] standingOverlaps = Physics.OverlapCapsule(
                    GetCapsuleBottomHemisphere(),
                    GetCapsuleTopHemisphere(height),
                    m_Controller.radius,
                    -1,
                    QueryTriggerInteraction.Ignore);

        foreach (Collider c in standingOverlaps)
        {
            if (c != m_Controller)
            {
                return true;
            }
        }

        return false;
    }

    public bool ShouldCrouch(Vector3 point)
    {
        Ray r = new Ray(point, Vector3.up);
        return Physics.SphereCast(r, m_Controller.radius, HeightStanding);
    }

    public void Crouch()
    {
        SetCrouchingState(true, false);
    }

    public float DistanceToGround()
    {
        RaycastHit hit;

        Physics.SphereCast(GetCapsuleBottomHemisphere(), m_Controller.radius, Vector3.down, out hit, Mathf.Infinity);
        return hit.distance;
    }
}
