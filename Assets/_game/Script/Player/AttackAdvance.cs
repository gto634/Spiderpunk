using UnityEngine;

public class AttackAdvance : MonoBehaviour
{
    [SerializeField] private MoveSystem moveSystem;
    [SerializeField] private Transform facing;
    [SerializeField] private Clock delayClock = new Clock { Duration = 0.3d };
    [SerializeField] private Clock durationClock = new Clock { Duration = 0.2d };
    [SerializeField, Min(0f)] private float speed = 2f;

    private Vector3 direction;

    internal void Begin()
    {
        if (!isActiveAndEnabled || moveSystem == null || facing == null)
            return;

        durationClock.ResetTimer();
        delayClock.StartTimer();
    }

    internal void Tick(double deltaTime)
    {
        if (!isActiveAndEnabled || moveSystem == null || facing == null)
            return;

        if (delayClock.IsRunning)
        {
            if (!delayClock.UpdateTimer(deltaTime, out double overflow))
                return;

            deltaTime = overflow;
            direction = Vector3.ProjectOnPlane(facing.forward, Vector3.up).normalized;
            durationClock.StartTimer();
        }

        if (!durationClock.IsRunning)
            return;

        double previousTime = durationClock.CurrentTime;
        durationClock.UpdateTimer(deltaTime, out _);
        float step = (float)(durationClock.CurrentTime - previousTime);
        if (step > 0f)
            moveSystem.Displace(direction * (speed * step));
    }

    internal void Stop()
    {
        delayClock.ResetTimer();
        durationClock.ResetTimer();
    }

    private void OnDisable() => Stop();
}
