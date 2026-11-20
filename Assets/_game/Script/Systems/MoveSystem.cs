using UnityEngine;
using NaughtyAttributes;

public class MoveSystem : MonoBehaviour
{
    [SerializeField] Transform target;

    [SerializeField, ReadOnly] float speed;
    [SerializeField, ReadOnly] Vector3 _direction;

    public float Speed { get => speed; set => speed = value; }

    public Vector3 Direction
    {
        get => _direction;
        set => _direction = value.normalized;
    }

    void Reset()
    {
        target = transform;
        speed = 2;
        _direction = Vector3.zero;
    }

    void Start()
    {
        ValidateSettings();
    }

    void ValidateSettings()
    {
        if (target != null)
            return;

        Debug.LogError("MoveSystem: assign a Target in the Inspector to enable movement.", this);
        enabled = false;
    }

    public void Update()
    {
        if (_direction != Vector3.zero)
            Displace(_direction * speed * Time.deltaTime);
    }

    internal void Displace(Vector3 offset)
    {
        if (isActiveAndEnabled && target != null)
            target.Translate(offset, Space.World);
    }
}
