using System;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;


public class AttackHitbox : MonoBehaviour
{
    const int OverlapSamplesPerAxis = 5;
    const float PointToleranceSquared = 0.000001f;

    [SerializeField] bool _hitOnceUntilExit;

    private Collider _collider;
    [ReadOnly] HashSet<Collider> _contacts = new HashSet<Collider>();
    [ReadOnly] HashSet<Transform> _hitTargets = new HashSet<Transform>();

    public Transform IgnoredRoot { get; set; }

    public Collider Collider
    {
        get
        {
            if (_collider == null)
            {
                _collider = GetComponent<Collider>();
            }

            return _collider;
        }
    }

    public event Action<Collider, Collider, Vector3?> ContactDetected;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        DetectContact(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (_hitOnceUntilExit) DetectContact(other);
    }

    public void DetectContact(Collider other, Vector3? sweptHitPoint = null)
    {
        if (!isActiveAndEnabled || !Collider.enabled) return;
        Transform root = other.transform.root;
        if (IgnoredRoot != null && root == IgnoredRoot.root) return;

        ContactDetected?.Invoke(Collider, other, sweptHitPoint);
    }

    internal static Vector3 GetImpactPoint(Collider source, Collider target, Vector3? sweptHitPoint)
    {
        Bounds sourceBounds = source.bounds;
        Bounds targetBounds = target.bounds;
        Vector3 min = Vector3.Max(sourceBounds.min, targetBounds.min);
        Vector3 max = Vector3.Min(sourceBounds.max, targetBounds.max);
        Vector3 overlapSize = max - min;
        if (overlapSize.x <= 0f || overlapSize.y <= 0f || overlapSize.z <= 0f)
            return sweptHitPoint ?? targetBounds.ClosestPoint(sourceBounds.center);

        Vector3 overlapCenter = (min + max) * 0.5f;
        if (!SupportsClosestPoint(source) || !SupportsClosestPoint(target))
            return sweptHitPoint ?? overlapCenter;

        Vector3 step = overlapSize / OverlapSamplesPerAxis;
        Vector3 sum = Vector3.zero;
        int insideCount = 0;
        for (int x = 0; x < OverlapSamplesPerAxis; x++)
        {
            for (int y = 0; y < OverlapSamplesPerAxis; y++)
            {
                for (int z = 0; z < OverlapSamplesPerAxis; z++)
                {
                    Vector3 point = min + Vector3.Scale(step, new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
                    if (IsInside(source, point) && IsInside(target, point))
                    {
                        sum += point;
                        insideCount++;
                    }
                }
            }
        }

        return insideCount > 0 ? sum / insideCount : sweptHitPoint ?? target.ClosestPoint(overlapCenter);
    }

    static bool SupportsClosestPoint(Collider collider) =>
        collider is BoxCollider or SphereCollider or CapsuleCollider or MeshCollider { convex: true };

    static bool IsInside(Collider collider, Vector3 point) =>
        (point - collider.ClosestPoint(point)).sqrMagnitude <= PointToleranceSquared;

    public bool TryRegisterHit(Collider target)
    {
        if (!_hitOnceUntilExit) return true;

        _contacts.Add(target);
        return _hitTargets.Add(target.transform.root);
    }

    private void OnTriggerExit(Collider other)
    {
        _contacts.Remove(other);
        Transform root = other.transform.root;
        foreach (Collider contact in _contacts)
        {
            if (contact != null && contact.transform.root == root) return;
        }

        _hitTargets.Remove(root);
    }

    public void ResetHits()
    {
        _hitTargets.Clear();
    }

    private void OnDisable()
    {
        _contacts.Clear();
        ResetHits();
    }
}
