using UnityEngine;
using NaughtyAttributes;

public class ShoulderCameraPlayer : MonoBehaviour
{
    [SerializeField] ShoulderCameraSystem shoulderCameraSystem;
    [SerializeField] Vector2 controllerSensitivity = new Vector2(0.2f, 0.1f);
    [SerializeField] Vector2 mouseSensitivity = new Vector2(0.2f, 0.1f);
    [SerializeField, ReadOnly] bool useGamepad;

    public Quaternion Orientation => shoulderCameraSystem != null ? shoulderCameraSystem.Orientation : Quaternion.identity;
    public Quaternion LookOrientation => shoulderCameraSystem != null ? shoulderCameraSystem.LookOrientation : Quaternion.identity;

    public void SetLookOrientation(Quaternion orientation)
    {
        if (shoulderCameraSystem != null)
            shoulderCameraSystem.SetLookOrientation(orientation);
    }

    public void SetAiming(bool aiming)
    {
        if (shoulderCameraSystem != null)
            shoulderCameraSystem.SetAiming(aiming);
    }

    public void SetLook(Vector2 input, bool fromGamepad)
    {
        useGamepad = fromGamepad;
        if (shoulderCameraSystem != null)
            shoulderCameraSystem.SetLook(input, useGamepad ? controllerSensitivity : mouseSensitivity);
    }

    void Reset()
    {
        shoulderCameraSystem = GetComponentInChildren<ShoulderCameraSystem>();
    }
}
