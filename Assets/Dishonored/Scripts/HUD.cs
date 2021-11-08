using UnityEngine.UI;
using UnityEngine;

public class HUD : MonoBehaviour
{
    [SerializeField]
    private RectTransform BlinkHUD;
    [SerializeField]
    private RectTransform PreBlinkHUD;
    [SerializeField]
    private RectTransform TimepieceHUD;
    [SerializeField]
    private RectTransform AbilitiesHUD;
    [SerializeField]
    private Image ManaUI;
    [SerializeField]
    private Color ManaUIFillColor;
    [SerializeField]
    private Color ManaUIEmptyColor;
    [SerializeField]
    private RectTransform InfoPanel;

    private GameManager m_GameManager;
    private Blink m_Blink;

    private bool m_ToggleInfoPanel;

    private float SelectedAbilityScale = 1.25f;
    private Image[] m_AbilityImages;
    private int m_LastAbilityIndex = 0;
    private int m_CurrentAbilityIndex = 0;

    private void Awake()
    {
        m_GameManager = FindObjectOfType<GameManager>();
        m_Blink = FindObjectOfType<Blink>();

        m_AbilityImages = new Image[4];
        m_AbilityImages[0] = AbilitiesHUD.GetChild(0).GetComponent<Image>();
        m_AbilityImages[1] = AbilitiesHUD.GetChild(1).GetComponent<Image>();
        m_AbilityImages[2] = AbilitiesHUD.GetChild(2).GetComponent<Image>();
        m_AbilityImages[3] = AbilitiesHUD.GetChild(3).GetComponent<Image>();
    }

    private void Start()
    {
        ManaUI.fillAmount = 1f;
        m_ToggleInfoPanel = true;

        ChangeCurrentAbility(Enums.Abilities.Timepiece, 0);

        m_AbilityImages[m_CurrentAbilityIndex].transform.localScale = Vector3.one * SelectedAbilityScale;
    }

    private void Update()
    {
        if (PlayerInputHandler.Instance.GetBackslashDown())
        {
            m_ToggleInfoPanel = !m_ToggleInfoPanel;
        }

        InfoPanel.gameObject.SetActive(m_ToggleInfoPanel);

        BlinkHUD.gameObject.SetActive(m_GameManager.IsBlinkActive() && !m_Blink.PreBlink);
        PreBlinkHUD.gameObject.SetActive(m_GameManager.IsBlinkActive() && m_Blink.PreBlink);
        TimepieceHUD.gameObject.SetActive(!m_GameManager.IsBlinkActive());

        ManaUI.fillAmount = m_GameManager.GetCurrentManaPercentage();

        if (m_GameManager.GetCurrentMana() < 10.0f)
        {
            ManaUI.color = ManaUIEmptyColor;
        }
        else
        {
            ManaUI.color = ManaUIFillColor;
        }
    }

    private void SwitchAbilityIndex(int previousAbilityIndex, int nextAbilityIndex)
    {
        m_LastAbilityIndex = previousAbilityIndex;
        m_CurrentAbilityIndex = nextAbilityIndex;

        m_AbilityImages[m_LastAbilityIndex].transform.localScale = Vector3.one;
        m_AbilityImages[m_CurrentAbilityIndex].transform.localScale = Vector3.one * SelectedAbilityScale;
    }

    public void ChangeCurrentAbility(Enums.Abilities ability, int index)
    {
        m_LastAbilityIndex = m_CurrentAbilityIndex;
        m_CurrentAbilityIndex = index;

        SwitchAbilityIndex(m_LastAbilityIndex, m_CurrentAbilityIndex);
    }
}
