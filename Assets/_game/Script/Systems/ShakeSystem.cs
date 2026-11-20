using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShakeSystem : MonoBehaviour
{
    const float Frequency = 10f;

    [SerializeField] Transform target;
    [SerializeField, Min(0f)] float duration = 0.3f;
    [SerializeField] Vector3 direction = Vector3.right;
    [SerializeField, Min(0f)] float intensity = 0.12f;

    Vector3 restPosition;
    double startedAt;
    bool isShaking;

    void Awake()
    {
        if (target == null)
            target = transform;
    }

    public void Play()
    {
        if (!isActiveAndEnabled) return;

        if (!isShaking)
            restPosition = target.localPosition;

        startedAt = Time.timeAsDouble;
        isShaking = true;
    }

    void LateUpdate()
    {
        if (!isShaking) return;

        float elapsed = (float)(Time.timeAsDouble - startedAt);
        if (elapsed >= duration)
        {
            StopShake();
            return;
        }

        float amplitude = intensity * (1f - elapsed / duration);
        float oscillation = -Mathf.Cos(elapsed * Frequency * 2f * Mathf.PI);
        target.localPosition = restPosition + direction.normalized * (amplitude * oscillation);
    }

    void OnDisable() => StopShake();

    void StopShake()
    {
        if (!isShaking) return;

        target.localPosition = restPosition;
        isShaking = false;
    }
}
