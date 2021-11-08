using System.Collections;
using UnityEngine;

public class Blink : MonoBehaviour
{
    [SerializeField]
    private float ManaCost;

    [Header("Teleport VFX")]
    [SerializeField]
    private Transform m_TeleportEffect;
    [SerializeField]
    private Transform m_TeleportEffectWall;

    [Header("Effect Parameters")]
    [SerializeField]
    private float MaxVignette = 0.3f;
    [SerializeField]
    private float MinSaturation = -75f;
    [SerializeField]
    private float PPDamp = 2f;
    [SerializeField]
    private float BlinkFov = 85f;
    [SerializeField]
    private ParticleSystem PreBlinkEffect;
    private ParticleSystem.EmissionModule m_PreBlinkEffectEmissionModule;
    private float m_DefaultFov;

    [Header("Collision Parameters")]
    [SerializeField]
    private float SphereCastRadius;
    [SerializeField]
    private float NormalOffset;
    [SerializeField]
    private float ScalingWallOffset = 0.8f;
    [SerializeField]
    private float CheckSpaceRadius = 1f;

    [Header("Debug Parameters")]
    [SerializeField]
    private float _debugGizmoSphereRadius = 0.05f;

    private Transform m_PlayerTransform;
    private Camera m_Camera;
    private CharacterController m_PlayerCharacterController;
    private PlayerCharacterController m_FPSControllerA;
    private PlayerCharacterController m_FPSControllerB;
    private GameManager m_GameManager;

    public struct PointInfo
    {
        public Vector3 point;
        public Vector3 normal;
        public Vector3 wallPoint;
        public Vector3 VFXPoint;
        public bool isScaling;

        public PointInfo(Vector3 point, Vector3 normal, Vector3 wallPoint, bool isScaling, Vector3 VFXPoint)
        {
            this.point = point;
            this.normal = normal;
            this.wallPoint = wallPoint;
            this.isScaling = isScaling;
            this.VFXPoint = VFXPoint;
        }

        public void Print()
        {
            Debug.Log
                (
                "Point: " + point + "\n" +
                "Normal: " + normal + "\n" +
                "WallPoint: " + wallPoint + "\n" +
                "IsScaling: " + isScaling + "\n" +
                "VFXpoint: " + VFXPoint + "\n"
                );
        }
    }

    [SerializeField]
    private int _capsuleDrawNumPoints;
    [SerializeField]
    private int _sphereCastDrawNumPoints;
    [SerializeField]
    private float m_DebugRayDistance;
    private Ray _debugRay;
    private Vector3 _debugSphereCastHitPoint;
    private Vector3 _debugHitUpPoint;
    private Vector3 _debugUpCastStart;
    private Vector3 _debugUpCastEnd;
    private Vector3 _debugFinalPoint;
    private Vector3 _debugCapsulePointUp;
    private Vector3 _debugCapsulePointBottom;

    private bool m_IsBlinking;
    private bool m_RightMousePressed;
    private bool m_RightMouseUp;
    private bool m_CancelBlink;
    
    private bool m_PreBlink;
    private bool m_GroundedAfterBlink = true;

    [SerializeField]
    private bool DebugSphereCast;
    [SerializeField]
    private bool DebugCheckSpaceCapsule;
    [SerializeField]
    private bool DebugPoints;

    public PlayerCharacterController FPSControllerA { get => m_FPSControllerA; }
    public PlayerCharacterController FPSControllerB { get => m_FPSControllerB; }
    public bool CancelBlink { get => m_CancelBlink; }
    public bool RightMousePressed { get => m_RightMousePressed; }
    public bool RightMouseUp { get => m_RightMouseUp; }
    public bool PreBlink { get => m_PreBlink; }
    public bool IsBlinking { get => m_IsBlinking; }

    private void Awake()
    {
        m_GameManager = FindObjectOfType<GameManager>();

        m_PlayerTransform = GameObject.FindGameObjectWithTag("PlayerA").transform;
        m_Camera = m_PlayerTransform.GetComponentInChildren<Camera>();

        m_PlayerCharacterController = m_PlayerTransform.GetComponent<CharacterController>();

        m_FPSControllerA = m_PlayerTransform.GetComponent<PlayerCharacterController>();
        m_FPSControllerB = GameObject.FindGameObjectWithTag("PlayerB").GetComponent<PlayerCharacterController>();
    }

    private void Start()
    {
        SphereCastRadius = m_FPSControllerA.GetComponent<CharacterController>().radius / 2f;
        CheckSpaceRadius = SphereCastRadius / 2f;
        NormalOffset = SphereCastRadius;
        ScalingWallOffset = m_FPSControllerA.PlayerHeightStanding / 2f;
        m_DefaultFov = m_PlayerTransform.GetComponentInChildren<Camera>().fieldOfView;

        m_TeleportEffect.gameObject.SetActive(false);
        m_TeleportEffectWall.gameObject.SetActive(false);

        m_PreBlinkEffectEmissionModule = PreBlinkEffect.emission;
        m_PreBlinkEffectEmissionModule.enabled = true;
        
        ResetParticles();
    }

    private void Update()
    {
        if(!m_CancelBlink)
        {
            m_RightMousePressed = PlayerInputHandler.Instance.GetRightMouseButtonHeld();
            m_RightMouseUp = PlayerInputHandler.Instance.GetRightMouseButtonReleased();
            m_PreBlink = m_RightMousePressed;
        }
        else
        {
            m_PreBlink = false;
            m_RightMousePressed = false;
            m_RightMouseUp = false;
        }

        if(!m_GroundedAfterBlink && m_FPSControllerA.isGrounded)
        {
            m_GroundedAfterBlink = true;
        }
        
        HandleCancelBlink();
    }

    public void HandleBlink(Vector3 point, float time, AnimationCurve fovCurve)
    {
        if (RightMouseUp || CancelBlink)
        {
            if(!FPSControllerA.isCrouching)
            {
                PPManager.Instance.ResetVignette();
            }

            PPManager.Instance.ResetColorGrading();
            
            TimeManager.Instance.ResetTime();
            DisableTeleportEffects();
            ResetParticles();

            if (CanBlink() && !CancelBlink)
            {
                PerformBlink(point, time, fovCurve);
                if (FPSControllerA.ShouldCrouch(point))
                {
                    FPSControllerA.Crouch();
                    FPSControllerB.Crouch();
                }
            }
        }
    }

    public void HandleCancelBlink()
    {
        if (PlayerInputHandler.Instance.GetCancelDown() && m_RightMousePressed)
        {
            m_CancelBlink = true;
        }
        else if (PlayerInputHandler.Instance.GetRightMouseButtonReleased())
        {
            m_CancelBlink = false;
        }
    }

    public void PerformBlink(Vector3 destination, float blinkTime, AnimationCurve fovCurve)
    {
        m_GameManager.UseMana(ManaCost);

        StartCoroutine(DoBlink(
            m_PlayerTransform.transform,
            m_PlayerTransform.transform.position,
            destination,
            blinkTime,
            fovCurve));
    }

    public void DisableTeleportEffects()
    {
        m_TeleportEffect.gameObject.SetActive(false);
        m_TeleportEffectWall.gameObject.SetActive(false);
    }

    public void ComputeEffectsPosition(PointInfo pointInfo)
    {
        float dotNormalUp = Vector3.Dot(pointInfo.normal, Vector3.up);

        if (pointInfo.wallPoint != Vector3.zero && dotNormalUp <= 0.01f && pointInfo.isScaling)
        {
            m_TeleportEffectWall.position = pointInfo.VFXPoint + pointInfo.normal * 0.1f - Vector3.up * 0.25f;
            m_TeleportEffectWall.rotation = 
                Quaternion.FromToRotation(m_TeleportEffect.forward * Mathf.Sign(pointInfo.normal.z), pointInfo.normal);

            m_TeleportEffect.gameObject.SetActive(false);
            m_TeleportEffectWall.gameObject.SetActive(true);
        }
        else
        {
            if (dotNormalUp <= 0.01f)
            {
                // pointing to a wall, set the position out of the wall
                m_TeleportEffect.position = pointInfo.point + pointInfo.normal * 0.25f * m_TeleportEffect.localScale.x;
            }
            else
            {
                // point in world space
                m_TeleportEffect.position = pointInfo.point;
            }

            m_TeleportEffect.gameObject.SetActive(true);
            m_TeleportEffectWall.gameObject.SetActive(false);
        }
    }

    public void DoEffects()
    {
        //m_PreBlinkEffectEmissionModule.enabled = true;
        PreBlinkEffect.Play();
        PPManager.Instance.VignetteSmoothness(MaxVignette, PPDamp);
        PPManager.Instance.SaturateColorGrading(MinSaturation, PPDamp);
    }

    public void ResetParticles()
    {
        //m_PreBlinkEffectEmissionModule.enabled = false;
        PreBlinkEffect.Stop();
        PreBlinkEffect.Clear();
    }

    public bool CanBlink()
    {
        return !m_IsBlinking && m_GameManager.GetCurrentMana() >= ManaCost && m_GroundedAfterBlink;
    }

    public float DistancePointPlayer(Vector3 point)
    {
        return Vector3.Distance(point, m_PlayerTransform.position);
    }

    public float GetBlinkTimeByDistance(float distance, BlinkSettings blinkSettings)
    {
        float speed = Util.Remap(distance, 
            blinkSettings.MinBlinkDistance, 
            blinkSettings.MaxBlinkDistance, 
            blinkSettings.MinBlinkTime, 
            blinkSettings.MaxBlinkTime);

        return speed;
    }

    public PointInfo GetTeleportPosition(float maxDistance)
    {
        float RayMaxDistance = maxDistance;
        Ray ray = m_Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        _debugRay = ray;
        Debug.DrawRay(ray.origin, ray.direction * RayMaxDistance, Color.black);

        PointInfo ret = new PointInfo
        {
            point = ray.origin + ray.direction * RayMaxDistance
        };

        RaycastHit hit;

        if (Physics.SphereCast(ray.origin, SphereCastRadius, ray.direction, out hit, RayMaxDistance, ~LayerMask.GetMask("Player")))
        {
            ret.point = hit.point;
            ret.normal = hit.normal;

            _debugSphereCastHitPoint = hit.point;

            float hitDotXZPlane = Vector3.Dot(hit.normal, Vector3.up);
            Vector3 currentPointNormal = hit.normal;

            // Aiming directly to a surface on the XZ Plane
            RaycastHit rayHit;
            if (hit.transform.CompareTag("Prop") || hit.transform.CompareTag("Floor"))
            {
                if (Physics.Raycast(ray, out rayHit, RayMaxDistance))
                {
                    if (Vector3.Dot(rayHit.normal, Vector3.up) >= 0.99f)
                    {
                        ret.point = rayHit.point;
                        ret.normal = rayHit.normal;
                        currentPointNormal = rayHit.normal;
                    }
                    Debug.DrawRay(rayHit.point, rayHit.normal, Color.yellow);
                }
            }

            // Aiming to a wall
            if (hitDotXZPlane < 0.98f)
            {
                _debugSphereCastHitPoint = hit.point;

                Vector3 offset = hit.normal * NormalOffset;
                Vector3 start = hit.point + Vector3.up * ScalingWallOffset - offset;
                Vector3 end = hit.point - offset;

                _debugUpCastStart = start;
                _debugUpCastEnd = end;

                ret.wallPoint = hit.point;

                Ray rayFromUp = new Ray(start, (end - start).normalized);

                float startEndDistance = Vector3.Distance(start, end);
                Debug.DrawRay(rayFromUp.origin, rayFromUp.direction * startEndDistance, Color.blue);

                // Ensures that pointing below an object (or near the edge)
                // won't make the player scale on top of it
                float endYHitPointYDelta = end.y - hit.point.y;
                RaycastHit rayFromUpHit;

                if (endYHitPointYDelta <= 0.01f && Physics.Raycast(rayFromUp, out rayFromUpHit, startEndDistance))
                {
                    bool upRaycastHasHit = rayFromUpHit.point != Vector3.zero;
                    ret.isScaling = upRaycastHasHit;
                    if (upRaycastHasHit)
                    {
                        _debugHitUpPoint = rayFromUpHit.point;
                        ret.isScaling = true;
                        ret.point = rayFromUpHit.point;
                        ret.VFXPoint = rayFromUpHit.point + offset;
                        currentPointNormal = rayFromUpHit.normal;
                    }
                }
            }

            // Check if the player is pointing to a wall
            if (Vector3.Dot(currentPointNormal, Vector3.up) >= 0.99f)
            {
                // Check if there is space to teleport to
                if (!CheckSpace(ret.point))
                {
                    ret.point = hit.point;
                    ret.normal = hit.normal;
                    ret.isScaling = false;
                }
            }
        }

        _debugFinalPoint = ret.point;

        return ret;
    }

    private bool CheckSpace(Vector3 point)
    {
        _debugCapsulePointUp = point + Vector3.up * m_FPSControllerA.PlayerHeightStanding / 2.0f;
        _debugCapsulePointBottom = point + Vector3.up * SphereCastRadius;
        Collider[] c = Physics.OverlapCapsule
                        (_debugCapsulePointUp,
                        _debugCapsulePointBottom,
                        CheckSpaceRadius);

        Ray ray = new Ray(_debugCapsulePointBottom, Vector3.up);
        float dst = Vector3.Distance(_debugCapsulePointUp, _debugCapsulePointBottom);
        bool sphereCast = Physics.SphereCast(ray, CheckSpaceRadius, dst, ~LayerMask.GetMask("Player"));
        return !sphereCast && c.Length == 0;
    }

    public IEnumerator DoBlink(Transform playerTransform, Vector3 from, Vector3 to, float duration, AnimationCurve fovCurve)
    {
        m_IsBlinking = true;
        float elapsedTime = 0;
        m_PlayerCharacterController.enabled = false;

        FPSControllerA.characterVelocity = Vector3.zero;
        FPSControllerB.characterVelocity = Vector3.zero;

        while (elapsedTime <= duration)
        {
            playerTransform.position = Vector3.Lerp(from, to, Util.EaseInSine(elapsedTime / duration));
            m_Camera.fieldOfView = Mathf.Lerp(m_DefaultFov, BlinkFov, fovCurve.Evaluate(elapsedTime / duration));
            yield return null;
            elapsedTime += Time.deltaTime;
        }

        playerTransform.position = to;
        m_IsBlinking = false;
        m_Camera.fieldOfView = m_DefaultFov;
        m_PlayerCharacterController.enabled = true;
        m_GroundedAfterBlink = m_FPSControllerA.isGrounded;
    }

    private void OnApplicationFocus(bool focus)
    {
        if (!focus)
        {
            TimeManager.Instance.ResetTime();
            PPManager.Instance.ResetAllEffects();
            DisableTeleportEffects();
            ResetParticles();
        }
    }

    private void OnDrawGizmos()
    {
        if (_sphereCastDrawNumPoints > 0 && DebugSphereCast)
        {
            Gizmos.color = Color.black;
            Vector3 rayStart = _debugRay.origin;
            Vector3 rayEnd = _debugRay.origin + _debugRay.direction * m_DebugRayDistance;
            float rayLength = Vector3.Distance(rayStart, rayEnd);
            float rayIncrement = rayLength / _sphereCastDrawNumPoints;
            float tRayIncrement = 0;
            for (int i = 0; i < _sphereCastDrawNumPoints + 1; i++)
            {
                Vector3 pos = _debugRay.GetPoint(tRayIncrement);
                Gizmos.DrawWireSphere(pos, SphereCastRadius);
                tRayIncrement += rayIncrement;
            }
        }

        if (DebugPoints)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_debugSphereCastHitPoint, _debugGizmoSphereRadius);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_debugHitUpPoint, _debugGizmoSphereRadius);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_debugUpCastEnd, _debugGizmoSphereRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_debugUpCastStart, _debugGizmoSphereRadius);

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(_debugFinalPoint, _debugGizmoSphereRadius);
        }


        if (m_PlayerTransform != null && _capsuleDrawNumPoints > 1 && DebugCheckSpaceCapsule)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(_debugCapsulePointUp, 0.1f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(_debugCapsulePointBottom, 0.1f);
            Gizmos.color = Color.black;

            float d = Vector3.Distance(_debugCapsulePointUp, _debugCapsulePointBottom);
            float increment = d / _capsuleDrawNumPoints;
            float tIncrement = 0;
            for (int i = 0; i < _capsuleDrawNumPoints; i++)
            {
                Vector3 pos = _debugCapsulePointBottom + Vector3.up * tIncrement;
                Gizmos.DrawWireSphere(pos, CheckSpaceRadius);
                tIncrement += increment;
            }
        }
    }
}
