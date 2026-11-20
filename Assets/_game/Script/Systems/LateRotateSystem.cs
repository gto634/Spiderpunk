using NaughtyAttributes;
using UnityEngine;

public class LateRotateSystem : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField, ReadOnly] Vector3 _direction;

    Quaternion worldRotationOffset = Quaternion.identity;
    Quaternion? animatedLocalRotation;
    Quaternion appliedLocalRotation;

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

    public void SetWorldRotationOffset(Quaternion rotation)
    {
        worldRotationOffset = rotation;
        if (rotation.Equals(Quaternion.identity))
            RestoreAnimatedRotation();
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

        Debug.LogError("LateRotateSystem: assign a Target in the Inspector to enable rotation.", this);
        enabled = false;
    }

    void Update()
    {
        RestoreAnimatedRotation();
    }

    void LateUpdate()
    {
        if (_direction != Vector3.zero)
            target.rotation = Quaternion.LookRotation(_direction, Vector3.up);

        if (worldRotationOffset.Equals(Quaternion.identity))
            return;

        animatedLocalRotation = target.localRotation;
        target.rotation = worldRotationOffset * target.rotation;
        appliedLocalRotation = target.localRotation;
    }

    void OnDisable()
    {
        RestoreAnimatedRotation();
    }

    void RestoreAnimatedRotation()
    {
        if (animatedLocalRotation.HasValue && target != null && target.localRotation.Equals(appliedLocalRotation))
            target.localRotation = animatedLocalRotation.Value;

        animatedLocalRotation = null;
    }
}
