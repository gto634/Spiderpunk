using UnityEngine;

[DisallowMultipleComponent]
public sealed class CaptiveSpring : MonoBehaviour
{
    [SerializeField] Rigidbody body;
    [SerializeField, Min(.1f)] float oscillationsPerSecond = 1.65f;
    [SerializeField, Min(.1f)] float damping = 2.8f;
    [SerializeField, Min(0f)] float impactSpeed = 2f;
    [SerializeField, Min(.1f)] float maxSpeed = 12f;

    Vector3 restPosition;
    Vector3 offset;
    Vector3 velocity;
    bool moving;

    void Awake() => restPosition = body.transform.localPosition;

    public void Recoil(Vector3 direction, float force)
    {
        if (direction.sqrMagnitude < .0001f)
            return;

        Vector3 impulse = direction.normalized * (Mathf.Max(1f, force) * impactSpeed);
        velocity = Vector3.ClampMagnitude(velocity + impulse, maxSpeed);
        moving = true;
    }

    void FixedUpdate()
    {
        if (!moving)
            return;

        float angularFrequency = 2f * Mathf.PI * oscillationsPerSecond;
        float step = Time.fixedDeltaTime;
        float sine = Mathf.Sin(angularFrequency * step) / angularFrequency;
        float cosine = Mathf.Cos(angularFrequency * step);
        float falloff = Mathf.Exp(-damping * step);
        Vector3 previousOffset = offset;
        offset = falloff * (previousOffset * cosine + (velocity + damping * previousOffset) * sine);
        velocity = falloff * (velocity * cosine -
            (damping * velocity + (angularFrequency * angularFrequency + damping * damping) * previousOffset) * sine);

        if (offset.sqrMagnitude < .000025f && velocity.sqrMagnitude < .0016f)
        {
            offset = Vector3.zero;
            velocity = Vector3.zero;
            moving = false;
        }

        body.MovePosition(GetRestPosition() + offset);
    }

    void OnDisable()
    {
        if (moving && body != null)
            body.position = GetRestPosition();
        offset = Vector3.zero;
        velocity = Vector3.zero;
        moving = false;
    }

    Vector3 GetRestPosition() => body.transform.parent != null
        ? body.transform.parent.TransformPoint(restPosition)
        : restPosition;
}
