using UnityEngine;
using UnityEngine.InputSystem;
using NaughtyAttributes;

[DefaultExecutionOrder(-5)]
public class OrbitCameraPlayer : MonoBehaviour
{
    [SerializeField] OrbitCameraSystem orbitCameraSystem;
    [SerializeField] Vector2 controllerSensitivity;
    [SerializeField] Vector2 mouseSensitivity;
    [SerializeField, ReadOnly] bool useGamepad = false;
    
    public bool UseGamepad
    {
        get => useGamepad;
        set
        {
            useGamepad = value;

            if (orbitCameraSystem != null)
                orbitCameraSystem.Sensitivity = useGamepad ? controllerSensitivity : mouseSensitivity;
        }
    }

    public Vector2 Direction
    {
        get => orbitCameraSystem != null ? orbitCameraSystem.Direction : Vector2.zero;
        set
        {
            if (orbitCameraSystem != null)
                orbitCameraSystem.Direction = value;
        }
    }
    
    public Quaternion Orientation
    {
        get => orbitCameraSystem != null ? orbitCameraSystem.GetCameraOrientation() : Quaternion.identity;
    }

    public void SetLookOrientation(Quaternion orientation)
    {
        if (orbitCameraSystem != null)
            orbitCameraSystem.SetLookOrientation(orientation);
    }

    void Reset()
    {
        orbitCameraSystem = GetComponentInChildren<OrbitCameraSystem>();
        
        controllerSensitivity = new Vector2(0.2f, 0.1f);
        mouseSensitivity = new Vector2(0.2f, 0.1f);
    }

    void Start()
    {
        if (orbitCameraSystem != null)
            return;

        Debug.LogError("OrbitCameraPlayer: assign Orbit Camera System in the Inspector.", this);
        enabled = false;
    }
}
