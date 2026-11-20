using NaughtyAttributes;
using UnityEngine;

public class RotateSystem : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField, ReadOnly] Vector3 _direction;

    public Transform Target => target;


    public Vector3 Direction
    {
        get => _direction;
        set => _direction = value;
    }

    public void SetLocalRotation(Quaternion rotation)
    {
        target.localRotation = rotation;
    }

    void Reset()
    {
        target = transform;
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

        Debug.LogError("RotateSystem: assign a Target in the Inspector to enable rotation.", this);
        enabled = false;
    }

    void Update()
    {
        RotateTowards(_direction);
    }

    public void RotateTowards(Vector3 direction)
    {
        if (target != null && direction != Vector3.zero)
            target.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }
}
