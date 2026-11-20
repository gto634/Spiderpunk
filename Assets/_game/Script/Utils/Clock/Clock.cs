using UnityEngine;

[System.Serializable]
public class Clock
{
    [SerializeField, Min(0f)] private double duration;

    public double Duration { get => duration; set => duration = value; }

    public double CurrentTime { get; private set; }

    public bool IsRunning { get; private set; }
    public bool IsFinished { get; private set; }

    public void StartTimer()
    {
        ResetTimer();
        IsRunning = true;
    }

    public void PauseTimer()
    {
        if (IsFinished) return;

        IsRunning = false;
    }

    public void ResumeTimer(bool _isRunning)
    {
        if (IsFinished) return;

        IsRunning = true;
    }

    public bool UpdateTimer(out double overflowTime) => UpdateTimer(Time.deltaTime, out overflowTime);

    public bool UpdateTimer(double deltaTime, out double _overflowTime)
    {
        _overflowTime = 0;

        if (IsFinished || !IsRunning)
            return IsFinished;

        double maxTime = System.Math.Max(0d, Duration);
        CurrentTime += deltaTime;

        if (CurrentTime >= maxTime)
        {
            _overflowTime = CurrentTime - maxTime;
            CurrentTime = maxTime;

            IsRunning = false;
            IsFinished = true;
        }

        return IsFinished;
    }

    public void ResetTimer()
    {
        CurrentTime = 0;
        IsRunning = false;
        IsFinished = false;
    }
}
