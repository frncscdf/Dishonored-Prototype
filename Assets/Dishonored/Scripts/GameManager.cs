using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private Map Map1;
    [SerializeField]
    private Map Map2;
    [SerializeField]
    private float Mana;

    [SerializeField]
    private int m_CurrentAbility;

    private Transform m_PlayerA;
    private Transform m_PlayerB;

    private Transform m_CurrentMap;
    private Vector3 m_Offest;
    private Vector3 m_OffestMap1Player;
    private Vector3 m_OffestMap2Player;

    private PlayerCharacterController m_PlayerController;

    private HUD m_HUD;

    private Blink m_Blink;
    private CorvoBlink m_CorvoBlink;
    private EmilyBlink m_EmilyBlink;
    private DaudBlink m_DaudBlink;

    private Timepiece m_TimepieceAbility;

    private MonoBehaviour[] m_Abilities;

    private bool m_Flip;
    private float m_CurrentMana;

    private void Awake()
    {
        m_PlayerA = GameObject.FindGameObjectWithTag("PlayerA").transform;
        m_PlayerB = GameObject.FindGameObjectWithTag("PlayerB").transform;

        m_PlayerController = m_PlayerA.GetComponent<PlayerCharacterController>();

        m_Blink = GetComponent<Blink>();
        m_CorvoBlink = GetComponent<CorvoBlink>();
        m_EmilyBlink = GetComponent<EmilyBlink>();
        m_DaudBlink = GetComponent<DaudBlink>();

        m_TimepieceAbility = GetComponent<Timepiece>();

        float playerHeightCrouched = m_PlayerController.PlayerHeightCrouching;

        Map1.Init(playerHeightCrouched + 0.01f);
        Map2.Init(playerHeightCrouched + 0.01f);

        m_Abilities = new MonoBehaviour[4];
        m_Abilities[0] = m_TimepieceAbility;
        m_Abilities[1] = m_CorvoBlink;
        m_Abilities[2] = m_EmilyBlink;
        m_Abilities[3] = m_DaudBlink;

        m_HUD = FindObjectOfType<HUD>();
    }

    private void Start()
    {
        m_Flip = false;
        m_CurrentMap = Map1.transform;
        m_CurrentMana = Mana;
        SwitchAbilities(0);
    }

    private void Update()
    {
        HandleMapPlayerPosition();
        int mouseScrollWheel = (int)PlayerInputHandler.Instance.GetMouseScrollDelta().y;
        if(mouseScrollWheel != 0 && !m_TimepieceAbility.IsTeleporting && !m_Blink.PreBlink)
        {
            SwitchAbilities(mouseScrollWheel);
        }

        if(m_CurrentMana < Mana)
        {
            m_CurrentMana += Time.deltaTime * 4f;
            m_CurrentMana = Mathf.Clamp(m_CurrentMana, 0, Mana);
        }
    }

    private void SwitchAbilities(int increment)
    {
        increment = Mathf.Clamp(increment, -1, 1);
        int previous = m_CurrentAbility;
        m_CurrentAbility += increment;

        if(m_CurrentAbility < 0)
        {
            m_CurrentAbility = m_Abilities.Length - 1;
        }
        else if(m_CurrentAbility > m_Abilities.Length - 1)
        {
            m_CurrentAbility = 0;
        }

        m_Abilities[m_CurrentAbility].enabled = true;

        for (int i = 0; i < m_Abilities.Length; i++)
        {
            if(i != m_CurrentAbility)
            {
                m_Abilities[i].enabled = false;
            }
        }

        m_HUD.ChangeCurrentAbility(EnumByIndex(m_CurrentAbility), m_CurrentAbility);
    }

    private Enums.Abilities EnumByIndex(int index)
    {
        if(index == 0)
        {
            return Enums.Abilities.Timepiece;
        }

        if (index == 1)
        {
            return Enums.Abilities.CorvoBlink;
        }

        if (index == 2)
        {
            return Enums.Abilities.EmilyBlink;
        }

        if (index == 3)
        {
            return Enums.Abilities.DaudBlink;
        }

        return Enums.Abilities.Timepiece;
    }

    private void HandleMapPlayerPosition()
    {
        m_OffestMap1Player = Map1.transform.position - m_PlayerA.position;
        m_OffestMap2Player = Map2.transform.position - m_PlayerA.position;

        m_Offest = m_CurrentMap.position - m_PlayerA.position;

        if (!m_Flip)
        {
            m_PlayerB.position = Map2.transform.position - m_OffestMap1Player;
        }
        else
        {
            m_PlayerB.position = Map1.transform.position - m_OffestMap2Player;
        }
    }

    public void UpdatePositions()
    {
        m_PlayerController.CharacterController.enabled = false;

        m_Flip = !m_Flip;
        if (m_Flip)
        {
            m_PlayerA.position = Map2.transform.position - m_Offest;
            m_CurrentMap = Map2.transform;
        }
        else
        {
            m_PlayerA.position = Map1.transform.position - m_Offest;
            m_CurrentMap = Map1.transform;
        }

        m_PlayerController.CharacterController.enabled = true;
    }

    public bool IsBlinkActive()
    {
        return m_CurrentAbility != 0;
    }

    public float GetCurrentMana()
    {
        return m_CurrentMana;
    }

    public float GetCurrentManaPercentage()
    {
        return m_CurrentMana / Mana;
    }

    public void UseMana(float amount)
    {
        m_CurrentMana -= amount;
    }
}
