using UnityEngine;

public class DashPlayer : MonoBehaviour
{
    [SerializeField, Min(0f)] float dashSpeed = 10f;
    [SerializeField, Min(0f)] float dashDuration = 1f;
    [SerializeField, Min(0f)] float dashCooldown = 1f;

    double dashEndTime;
    double nextDashTime;

    public bool IsDashing => isActiveAndEnabled && Time.timeAsDouble < dashEndTime;
    public float Duration => dashDuration;
    public Vector3 Direction { get; private set; }

    public float CurrentSpeed => dashSpeed * SpeedFactor;

    public float SpeedFactor
    {
        get
        {
            if (!IsDashing || dashDuration <= 0f)
                return 0f;

            float remaining = (float)((dashEndTime - Time.timeAsDouble) / dashDuration);
            return Mathf.SmoothStep(0f, 1f, remaining);
        }
    }

    public bool TryStartDash(Vector3 direction)
    {
        double now = Time.timeAsDouble;
        if (!isActiveAndEnabled || direction == Vector3.zero || dashDuration <= 0f || now < nextDashTime)
            return false;

        Direction = direction.normalized;
        dashEndTime = now + dashDuration;
        nextDashTime = dashEndTime + dashCooldown;
        return true;
    }

    void OnDisable()
    {
        dashEndTime = 0d;
        nextDashTime = 0d;
        Direction = Vector3.zero;
    }
}
