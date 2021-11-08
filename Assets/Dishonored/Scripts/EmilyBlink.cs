using UnityEngine;

public class EmilyBlink : MonoBehaviour
{
    private Blink m_Blink;

    [Header("Blink Parameters")]
    [SerializeField]
    private BlinkSettings BlinkSettings;

    private Blink.PointInfo m_PointInfo;

    private float m_DistanceToBlinkPoint;
    private float m_BlinkTime;

    private void Awake()
    {
        m_Blink = FindObjectOfType<Blink>();
    }

    private void Update()
    {
        m_PointInfo = m_Blink.GetTeleportPosition(BlinkSettings.MaxBlinkDistance);
        m_DistanceToBlinkPoint = m_Blink.DistancePointPlayer(m_PointInfo.point);
        m_BlinkTime = m_Blink.GetBlinkTimeByDistance(m_DistanceToBlinkPoint, BlinkSettings);

        HandleBlink();
    }

    private void HandleBlink()
    {
        if (m_Blink.RightMousePressed && m_Blink.CanBlink() && !m_Blink.CancelBlink)
        {
            float dstGround = m_Blink.FPSControllerA.DistanceToGround();
            if (dstGround > m_Blink.FPSControllerA.CharacterController.height)
            {
                Vector3 vel = m_Blink.FPSControllerA.characterVelocity;
                m_Blink.FPSControllerA.characterVelocity = Vector3.up * vel.y * 0.95f;
                m_Blink.FPSControllerB.characterVelocity = Vector3.up * vel.y * 0.95f;
                TimeManager.Instance.ScaleTimeHalf();
                m_Blink.DoEffects();
            }
            else
            {
                TimeManager.Instance.ResetTime();
                PPManager.Instance.ResetColorGrading();
                if (!m_Blink.FPSControllerA.isCrouching)
                {
                    PPManager.Instance.ResetVignette();
                }
                m_Blink.ResetParticles();
            }
            m_Blink.ComputeEffectsPosition(m_PointInfo);
        }

        m_Blink.HandleBlink(m_PointInfo.point, m_BlinkTime, BlinkSettings.FovCurve);
    }

    private bool m_IsQuitting = false;

    private void OnApplicationQuit()
    {
        m_IsQuitting = true;
    }

    private void OnDisable()
    {
        if (!m_IsQuitting)
            m_Blink.DisableTeleportEffects();
    }
}
