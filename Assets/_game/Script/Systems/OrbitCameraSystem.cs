using Unity.Cinemachine;
using UnityEngine;
using NaughtyAttributes;

public class OrbitCameraSystem : MonoBehaviour
{
    [SerializeField] CinemachineOrbitalFollow orbitalFollow;
    [SerializeField, ReadOnly] Vector2 sensitivity;
    [SerializeField, ReadOnly] Vector2 direction;

    CinemachineVirtualCameraBase virtualCamera;

    public Vector2 Direction
    {
        get => direction;
        set => direction = value;
    }

    public Vector2 Sensitivity
    {
        get => sensitivity; 
        set => sensitivity = value;
    }
    
    void Reset()
    {
        orbitalFollow = transform.root.GetComponentInChildren<CinemachineOrbitalFollow>();
        
        sensitivity = new Vector2(0.2f, 0.1f);
    }
    
    void Start()
    {
        if (!ValidateConfiguration())
            enabled = false;
    }

    bool ValidateConfiguration()
    {
        if (orbitalFollow == null)
        {
            Debug.LogError("OrbitCameraSystem: assign a CinemachineOrbitalFollow to Orbital Follow in the Inspector.", this);
            return false;
        }

        virtualCamera = orbitalFollow.VirtualCamera;
        if (virtualCamera == null)
        {
            Debug.LogError("OrbitCameraSystem: the assigned CinemachineOrbitalFollow must be on a Cinemachine camera.", this);
            return false;
        }

        return true;
    }

    public Quaternion GetCameraOrientation()
    {
        if (virtualCamera != null && virtualCamera.PreviousStateIsValid)
            return virtualCamera.State.GetFinalOrientation();

        return orbitalFollow != null ? orbitalFollow.transform.rotation : Quaternion.identity;
    }

    public void SetLookOrientation(Quaternion orientation)
    {
        if (virtualCamera == null || !virtualCamera.isActiveAndEnabled || orbitalFollow.FollowTarget == null)
            return;

        virtualCamera.InternalUpdateCameraState(Vector3.up, -1f);

        CameraState state = virtualCamera.State;
        Vector3 orbitCenter = state.HasLookAt() ? state.ReferenceLookAt : orbitalFollow.FollowTargetPosition;
        float distance = Mathf.Max(1f, Vector3.Distance(state.GetFinalPosition(), orbitCenter));
        Vector3 offset = orientation * (Vector3.back * distance);
        virtualCamera.ForceCameraPosition(orbitCenter + offset, orientation);

        if (orbitalFollow.OrbitStyle == CinemachineOrbitalFollow.OrbitStyles.ThreeRing)
        {
            var axis = orbitalFollow.VerticalAxis;
            float alongSpline = axis.Value <= axis.Center
                ? Mathf.InverseLerp(axis.Range.x, axis.Center, axis.Value) * 0.5f
                : 0.5f + Mathf.InverseLerp(axis.Center, axis.Range.y, axis.Value) * 0.5f;
            orbitalFollow.VerticalAxis.Value = Mathf.Lerp(axis.Range.x, axis.Range.y, alongSpline);
        }

        virtualCamera.InternalUpdateCameraState(Vector3.up, -1f);
    }

    public void Update()
    {
        Vector2 delta = Vector2.Scale(direction, sensitivity);
        orbitalFollow.HorizontalAxis.Value = orbitalFollow.HorizontalAxis.ClampValue(
            orbitalFollow.HorizontalAxis.Value + delta.x);
        orbitalFollow.VerticalAxis.Value = orbitalFollow.VerticalAxis.ClampValue(
            orbitalFollow.VerticalAxis.Value - delta.y);
    }
}
