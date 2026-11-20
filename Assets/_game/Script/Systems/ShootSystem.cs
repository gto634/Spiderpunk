using System;
using UnityEngine;

[DefaultExecutionOrder(20)]
public class ShootSystem : MonoBehaviour
{
    [SerializeField] Transform _shootPoint;
    [SerializeField] Transform _aimCamera;
    [SerializeField, Min(0f)] float _aimDistance = 2.5f;
    [SerializeField] WebShotThreads _shotThreads;
    [SerializeField] GameObject _prefab;
    [SerializeField] Transform _owner;
    [SerializeField] BoxCollider _detectionHitbox;
    [SerializeField] Clock delayClock = new Clock();

    readonly EnemyDetectionBox enemyDetection = new EnemyDetectionBox();
    Transform pendingShootPoint;

    public bool CanShoot => isActiveAndEnabled && _shootPoint != null && _prefab != null && !HasPendingShoot;
    public bool HasPendingShoot => delayClock.IsRunning;
    public GameObject LastProjectile { get; private set; }
    public Transform ShootPoint => _shootPoint;
    public event Action OnShoot;
    public event Action<Transform> OnEnemyDetected;

    void Awake()
    {
        if (_owner == null)
            _owner = transform.root;
        FitDetectionRange(_prefab);
    }

    void LateUpdate()
    {
        if (HasPendingShoot && delayClock.UpdateTimer(out _))
            CreateProjectile(pendingShootPoint);
        else
            AimShootPoint();
    }

    public void Shoot() => Shoot(_shootPoint);

    public void ShootImmediately()
    {
        if (CanShoot)
            CreateProjectile(_shootPoint);
    }

    public void Shoot(Transform shootPoint)
    {
        if (!isActiveAndEnabled || shootPoint == null || _prefab == null || HasPendingShoot)
            return;

        pendingShootPoint = shootPoint;
        delayClock.StartTimer();
        if (delayClock.Duration <= 0d)
        {
            delayClock.UpdateTimer(0d, out _);
            CreateProjectile(pendingShootPoint);
        }
    }

    void CreateProjectile(Transform shootPoint)
    {
        if (shootPoint == null)
        {
            CancelShoot();
            return;
        }

        if (shootPoint == _shootPoint)
            AimShootPoint();

        GameObject instance = Instantiate(_prefab, shootPoint.position, shootPoint.rotation);
        pendingShootPoint = null;
        LastProjectile = instance;
        if (instance.TryGetComponent(out Projectile projectile))
            projectile.Initialize(_owner);
        if (_shotThreads != null)
            _shotThreads.Play(instance.transform);
        DetectEnemies(shootPoint, instance);
        OnShoot?.Invoke();
    }

    bool FitDetectionRange(GameObject source)
    {
        if (_detectionHitbox == null || source == null || !source.TryGetComponent(out BrainProjectile projectile))
            return false;

        float scale = _detectionHitbox.transform.lossyScale.z;
        if (Mathf.Approximately(scale, 0f))
            return false;

        float length = projectile.Range / scale;
        Vector3 size = _detectionHitbox.size;
        Vector3 center = _detectionHitbox.center;
        size.z = Mathf.Abs(length);
        center.z = length * 0.5f;
        _detectionHitbox.size = size;
        _detectionHitbox.center = center;
        return projectile.Range > 0f;
    }

    void DetectEnemies(Transform shootPoint, GameObject projectile)
    {
        if (!FitDetectionRange(projectile) || OnEnemyDetected == null || !_detectionHitbox.gameObject.activeInHierarchy)
            return;

        Transform area = _detectionHitbox.transform;
        Vector3 center = area.TransformPoint(_detectionHitbox.center);
        Quaternion rotation = area.rotation;
        if (shootPoint != _shootPoint)
        {
            Transform referencePoint = _shootPoint != null ? _shootPoint : area;
            Quaternion muzzleRotation = shootPoint.rotation * Quaternion.Inverse(referencePoint.rotation);
            center = shootPoint.position + muzzleRotation * (center - referencePoint.position);
            rotation = muzzleRotation * rotation;
        }

        var enemies = enemyDetection.Query(_detectionHitbox, _owner, center, rotation);
        for (int i = 0; i < enemies.Count; i++)
            OnEnemyDetected?.Invoke(enemies[i]);
    }

    void AimShootPoint()
    {
        if (_shootPoint == null || _aimCamera == null || !_aimCamera.gameObject.activeInHierarchy)
            return;

        Vector3 aimPoint = _aimCamera.position + _aimCamera.forward * _aimDistance;
        Vector3 direction = aimPoint - _shootPoint.position;
        if (direction.sqrMagnitude > 0.000001f)
            _shootPoint.rotation = Quaternion.LookRotation(direction, _aimCamera.up);
    }

    public void CancelShoot()
    {
        delayClock.ResetTimer();
        pendingShootPoint = null;
    }

    void OnDisable() => CancelShoot();
}

