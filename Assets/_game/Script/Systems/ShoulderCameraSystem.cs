using System;
using UnityEngine;
using NaughtyAttributes;

[DefaultExecutionOrder(-5)]
public class ShoulderCameraSystem : MonoBehaviour
{
    [SerializeField] RotateSystem rootRotateSystem;
    [SerializeField] RotateSystem spineRotateSystem;
    [SerializeField] Transform shoulderTarget;
    [SerializeField] Vector2 pitchLimits = new Vector2(-60f, 60f);

    [SerializeField, ReadOnly] Vector2 sensitivity;
    [SerializeField, ReadOnly] Vector2 direction;

    public event Action<Quaternion> OnShoulderRotation;

    Transform root;
    Transform shoulderPivot;
    Quaternion neutralPivotRotation;
    Vector3 neutralLookDirection;
    bool isAiming;
    float pitch;

    public Quaternion Orientation => root != null ? root.rotation : Quaternion.identity;
    public Quaternion LookOrientation => shoulderTarget != null ? shoulderTarget.rotation : Orientation;

    public void SetLookOrientation(Quaternion orientation)
    {
        if (root == null || shoulderPivot == null || shoulderTarget == null)
            return;

        Vector3 lookDirection = orientation * Vector3.forward;
        float projectedLength = new Vector2(neutralLookDirection.y, neutralLookDirection.z).magnitude;
        float neutralPitch = Mathf.Atan2(-neutralLookDirection.y, neutralLookDirection.z) * Mathf.Rad2Deg;
        float lookPitch = Mathf.Asin(Mathf.Clamp(-lookDirection.y / projectedLength, -1f, 1f)) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(lookPitch - neutralPitch, pitchLimits.x, pitchLimits.y);

        Vector3 pitchedLook = Quaternion.AngleAxis(pitch, Vector3.right) * neutralLookDirection;
        Vector3 horizontalLook = Vector3.ProjectOnPlane(lookDirection, Vector3.up);
        if (horizontalLook.sqrMagnitude > 0.0001f)
        {
            float yaw = Vector3.SignedAngle(Vector3.ProjectOnPlane(pitchedLook, Vector3.up), horizontalLook, Vector3.up);
            root.rotation = Quaternion.AngleAxis(yaw, Vector3.up);
        }

        ApplyPitch();
    }

    public void SetLook(Vector2 input, Vector2 lookSensitivity)
    {
        direction = input;
        sensitivity = lookSensitivity;
    }

    public void SetAiming(bool aiming)
    {
        if (isAiming == aiming)
            return;

        isAiming = aiming;
        if (aiming)
            return;

        if (root != null && shoulderPivot != null)
            spineRotateSystem.SetLocalRotation(neutralPivotRotation);
        pitch = 0f;
        direction = Vector2.zero;
        OnShoulderRotation?.Invoke(Quaternion.identity);
    }

    void Awake()
    {
        root = rootRotateSystem != null ? rootRotateSystem.Target : null;
        shoulderPivot = spineRotateSystem != null ? spineRotateSystem.Target : null;
        if (root == null || shoulderPivot == null)
        {
            Debug.LogError("ShoulderCameraSystem: assign RotateSystems targeting the root and shoulder pivot.", this);
            enabled = false;
            return;
        }

        neutralPivotRotation = shoulderPivot.localRotation;
        if (shoulderTarget != null)
            neutralLookDirection = root.InverseTransformDirection(shoulderTarget.forward);
    }

    void Update()
    {
        if (!isAiming)
            return;

        Vector2 delta = Vector2.Scale(direction, sensitivity);
        if (delta.x != 0f)
            root.Rotate(Vector3.up, delta.x, Space.World);
        pitch = Mathf.Clamp(pitch - delta.y, pitchLimits.x, pitchLimits.y);
    }

    void LateUpdate()
    {
        if (!isAiming)
            return;
        ApplyPitch();
        OnShoulderRotation?.Invoke(Quaternion.AngleAxis(pitch, root.right));
    }

    void ApplyPitch()
    {
        Vector3 pivotAxis = shoulderPivot.parent.InverseTransformDirection(root.right);
        spineRotateSystem.SetLocalRotation(Quaternion.AngleAxis(pitch, pivotAxis) * neutralPivotRotation);
    }

    void OnDisable()
    {
        SetAiming(false);
    }
}
