using System.Collections;
using UnityEngine;

public class Timepiece : MonoBehaviour
{
    [Header("Effect Parameters")]
    [SerializeField]
    private float MinSaturation = -75f;
    [SerializeField]
    private float TravelFov = 85f;
    private float m_DefaultFov;
    [SerializeField]
    private float MaxVignette = 0.3f;
    [SerializeField]
    private float TravelTime;
    [SerializeField]
    private AnimationCurve EffectsCurve;
    [SerializeField]
    private ParticleSystem PostTravelParticles;

    private GameManager m_GameManager;
    private Transform m_RenderTextureHolder;
    private Camera m_Camera;
    private PlayerCharacterController m_PlayerBController;

    private bool m_PortalActive;
    private bool m_IsTeleporting;
    private bool m_CanTravel;
    private Vector3 m_TimepieceDefaultPosition;

    public bool IsTeleporting { get => m_IsTeleporting; set => m_IsTeleporting = value; }

    private void Awake()
    {
        m_GameManager = FindObjectOfType<GameManager>();

        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
        Shader.SetGlobalTexture("_TimePieceTexture", rt);
        Transform secondPlayer = GameObject.FindGameObjectWithTag("PlayerB").transform;
        secondPlayer.GetChild(0).GetComponent<Camera>().targetTexture = rt;
        m_PlayerBController = secondPlayer.GetComponent<PlayerCharacterController>();

        m_Camera = GameObject.FindGameObjectWithTag("PlayerA").transform.GetChild(0).GetComponent<Camera>();

        m_PortalActive = false;
        m_DefaultFov = m_Camera.fieldOfView;

        m_RenderTextureHolder = GameObject.FindGameObjectWithTag("PlayerA").transform.GetChild(0).GetChild(0).transform;
        m_RenderTextureHolder.gameObject.SetActive(false);

        m_TimepieceDefaultPosition = m_RenderTextureHolder.localPosition;
    }

    private void Update()
    {
        m_CanTravel = !m_PlayerBController.IsObstructed(m_PlayerBController.PlayerHeightStanding);
        HandleTimePiece();
    }

    private void OnDisable()
    {
        m_PortalActive = false;
        if(m_RenderTextureHolder)
        {
            m_RenderTextureHolder.gameObject.SetActive(false);
        }
    }

    private void HandleTimePiece()
    {
        if (PlayerInputHandler.Instance.GetCancelDown())
        {
            m_PortalActive = !m_PortalActive;
        }

        m_RenderTextureHolder.gameObject.SetActive(m_PortalActive);

        if (PlayerInputHandler.Instance.GetLeftMouseButtonDown() && !m_IsTeleporting && m_CanTravel)
        {
            StartCoroutine(Travel(TravelTime));
        }
    }

    public IEnumerator Travel(float duration)
    {
        m_IsTeleporting = true;

        float elapsedTime = 0f;
        
        while (elapsedTime <= duration)
        {
            float t = elapsedTime / duration;
            m_Camera.fieldOfView = Mathf.Lerp(m_DefaultFov, TravelFov, EffectsCurve.Evaluate(t));
            m_RenderTextureHolder.transform.localPosition =
                Vector3.Lerp(m_TimepieceDefaultPosition, m_TimepieceDefaultPosition + Vector3.forward * 0.4f, EffectsCurve.Evaluate(t));
            PPManager.Instance.Vignette.smoothness.value = Mathf.Lerp(0f, MaxVignette, EffectsCurve.Evaluate(t));
            PPManager.Instance.ColorGrading.saturation.value = Mathf.Lerp(0f, MinSaturation, EffectsCurve.Evaluate(t));

            yield return null;

            elapsedTime += Time.deltaTime;
        }

        m_GameManager.UpdatePositions();
        PostTravelParticles.Play();
        m_Camera.fieldOfView = m_DefaultFov;
        m_RenderTextureHolder.transform.localPosition = m_TimepieceDefaultPosition;

        PPManager.Instance.Vignette.smoothness.value = 0f;
        PPManager.Instance.ColorGrading.saturation.value = 0f;
        
        m_IsTeleporting = false;
    }
}
