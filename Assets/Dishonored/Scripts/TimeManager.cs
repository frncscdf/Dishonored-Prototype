using UnityEngine;

public class TimeManager : Singleton<TimeManager>
{
    private const float m_HalfTimeScale = 0.5f;
    private bool m_TimeStopped;

    public bool TimeStopped { get => m_TimeStopped; }

    public void ScaleTimeHalf()
    {
        Time.timeScale = m_HalfTimeScale;
        m_TimeStopped = true;
    }

    public void StopTime()
    {
        Time.timeScale = 0f;

        m_TimeStopped = true;
    }

    public void ResetTime()
    {
        Time.timeScale = 1.0f;

        m_TimeStopped = false;
    }
}
