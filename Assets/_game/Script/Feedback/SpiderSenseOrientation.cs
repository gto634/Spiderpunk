using UnityEngine;

public class SpiderSenseOrientation : MonoBehaviour
{
    [SerializeField] private Transform _head;
    [SerializeField] private ParticleSystem _particleSystem;
    [SerializeField] private Camera _camera;
    [SerializeField] private float _angleOffset = 90f;
    [SerializeField] private float _shapeYawOffset = 0f;

    private ParticleSystem.Particle[] _particles;

    public bool IsFinished => !_particleSystem.IsAlive(true);

    private void Awake()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        _particles = new ParticleSystem.Particle[_particleSystem.main.maxParticles];
    }

    private void LateUpdate()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null)
                return;
        }

        if (_head == null)
            UpdateShapeYaw();

        int count = _particleSystem.GetParticles(_particles);

        if (count == 0)
        {
            return;
        }

        Transform camTransform = _camera.transform;
        Vector3 center = _head != null ? _head.position : transform.position;
        ParticleSystem.MainModule main = _particleSystem.main;
        Transform simulationTransform = main.simulationSpace switch
        {
            ParticleSystemSimulationSpace.Local => _particleSystem.transform,
            ParticleSystemSimulationSpace.Custom => main.customSimulationSpace,
            _ => null
        };
        Matrix4x4 simulationToWorld = simulationTransform != null
            ? simulationTransform.localToWorldMatrix
            : Matrix4x4.identity;

        for (int i = 0; i < count; i++)
        {
            Vector3 worldPos = simulationToWorld.MultiplyPoint3x4(_particles[i].position);
            Vector3 worldDir = center - worldPos;
            Vector3 camDir = camTransform.InverseTransformDirection(worldDir);

            float angle = Mathf.Atan2(camDir.y, camDir.x) * Mathf.Rad2Deg;

            _particles[i].rotation = -angle + _angleOffset;
        }

        _particleSystem.SetParticles(_particles, count);
    }

    private void UpdateShapeYaw()
    {
        Vector3 toCamera = _camera.transform.position - transform.position;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude < 0.0001f)
            return;

        Vector3 localDir = transform.InverseTransformDirection(toCamera);
        float yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        ParticleSystem.ShapeModule shape = _particleSystem.shape;
        Vector3 rotation = shape.rotation;
        rotation.y = yaw + _shapeYawOffset;
        shape.rotation = rotation;
    }

    public void Stop()
    {
        if (_particleSystem != null)
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    [ContextMenu("Play")]
    public void Play()
    {
        _particleSystem.Play();
    }
}
