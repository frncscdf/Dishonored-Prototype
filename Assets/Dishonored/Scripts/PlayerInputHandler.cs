using UnityEngine;

public class PlayerInputHandler : Singleton<PlayerInputHandler>
{
    [Tooltip("Sensitivity multiplier for moving the camera around")]
    public float lookSensitivity = 1f;
    [Tooltip("Used to flip the vertical input axis")]
    public bool invertYAxis = false;
    [Tooltip("Used to flip the horizontal input axis")]
    public bool invertXAxis = false;

    private Blink m_Blink;

    private void Awake()
    {
        m_Blink = FindObjectOfType<Blink>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool CanProcessInput()
    {
        return Cursor.lockState == CursorLockMode.Locked && !m_Blink.IsBlinking;
    }

    public Vector3 GetMoveInput()
    {
        if (CanProcessInput())
        {
            Vector3 move = 
                new Vector3(
                    Input.GetAxisRaw(InputConstants.k_AxisNameHorizontal),
                    0f,
                    Input.GetAxisRaw(InputConstants.k_AxisNameVertical));

            move = Vector3.ClampMagnitude(move, 1);

            return move;
        }

        return Vector3.zero;
    }

    public float GetLookInputsHorizontal()
    {
        return GetMouseLookAxis(InputConstants.k_MouseAxisNameHorizontal, invertXAxis);
    }

    public float GetLookInputsVertical()
    {
        return GetMouseLookAxis(InputConstants.k_MouseAxisNameVertical, invertYAxis);
    }

    public bool GetJumpInputDown()
    {
        if (CanProcessInput())
        {
            return Input.GetKeyDown(InputConstants.k_Jump);
        }

        return false;
    }

    public bool GetJumpInputHeld()
    {
        if (CanProcessInput())
        {
            return Input.GetKey(InputConstants.k_Jump);
        }

        return false;
    }

    public bool GetSprintInputHeld()
    {
        if (CanProcessInput())
        {
            return Input.GetKey(InputConstants.k_Sprint);
        }

        return false;
    }

    public bool GetCrouchInputDown()
    {
        if (CanProcessInput())
        {
            return Input.GetKeyDown(InputConstants.k_Crouch);
        }

        return false;
    }

    public bool GetLeftMouseButtonDown()
    {
        if (CanProcessInput())
        {
            return Input.GetKeyDown(InputConstants.k_LeftMouseButton);
        }

        return false;
    }

    public bool GetRightMouseButtonHeld()
    {
        if (CanProcessInput())
        {
            return Input.GetKey(InputConstants.k_RightMouseButton);
        }

        return false;
    }

    public bool GetRightMouseButtonReleased()
    {
        if (CanProcessInput())
        {
            return Input.GetKeyUp(InputConstants.k_RightMouseButton);
        }

        return false;
    }

    public bool GetBackslashDown()
    {
        if (CanProcessInput())
        {
            return Input.GetKeyDown(InputConstants.k_Backslash);
        }

        return false;
    }

    public bool GetCancelDown()
    {
        if (CanProcessInput())
        {
            return Input.GetKeyDown(InputConstants.k_Cancel);
        }

        return false;
    }

    public Vector2 GetMouseScrollDelta()
    {
        if (CanProcessInput())
        {
            return Input.mouseScrollDelta;
        }

        return Vector2.zero;
    }

    float GetMouseLookAxis(string mouseInputName, bool invert)
    {
        if (CanProcessInput())
        {
            float i = Input.GetAxisRaw(mouseInputName);

            if (invert)
                i *= -1f;

            i *= lookSensitivity;

            return i;
        }

        return 0f;
    }
}
