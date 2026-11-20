using System;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class BrainProjectile : MonoBehaviour
{
    [SerializeField] Projectile projectile;
    [SerializeField] MoveSystem _moveSystem;
    [SerializeField] AttackSystem _attackSystem;
    [SerializeField] AttackHitbox hitbox;
    [SerializeField] SphereCollider hitCollider;
    [SerializeField] WebProjectileFeedback feedback;
    [SerializeField, Min(0f)] float speed = 10f;
    [SerializeField] Clock lifeClock = new Clock();

    public float Range => speed * (float)lifeClock.Duration;

    RaycastHit[] contacts = new RaycastHit[16];
    bool finished;
    Vector3 impactPoint;
    Vector3 impactNormal;

    void Awake()
    {
        if (projectile != null && _moveSystem != null && _attackSystem != null && hitbox != null && hitCollider != null)
            return;
        Debug.LogError("BrainProjectile: assign Projectile, Move System, Attack System and the sphere hitbox.", this);
        enabled = false;
    }

    void OnEnable() => _attackSystem.OnHit += OnHit;

    void OnDisable()
    {
        if (_attackSystem != null)
            _attackSystem.OnHit -= OnHit;
        if (_moveSystem != null)
            _moveSystem.Direction = Vector3.zero;
    }

    void Start()
    {
        hitbox.IgnoredRoot = projectile.Owner;
        _moveSystem.Speed = speed;
        _moveSystem.Direction = projectile.transform.forward;
        lifeClock.StartTimer();
        _attackSystem.Attack(ignoreDelay: true);
    }

    void Update()
    {
        if (finished)
            return;
        if (lifeClock.UpdateTimer(out _))
        {
            Finish();
            return;
        }

        Vector3 worldDirection = projectile.transform.forward;
        _moveSystem.Direction = worldDirection;
        float distance = speed * Time.deltaTime;
        if (distance <= 0f)
            return;
        Vector3 center = hitCollider.transform.TransformPoint(hitCollider.center);
        Vector3 scale = hitCollider.transform.lossyScale;
        float radius = hitCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        int count;
        while (true)
        {
            count = Physics.SphereCastNonAlloc(center, radius, worldDirection,
                contacts, distance, Physics.AllLayers, QueryTriggerInteraction.Collide);
            if (count < contacts.Length)
                break;
            Array.Resize(ref contacts, contacts.Length * 2);
        }

        int closest = -1;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
            if (contacts[i].distance < nearest && CanCollide(contacts[i].collider))
            {
                closest = i;
                nearest = contacts[i].distance;
            }
        if (closest < 0)
            return;
        RaycastHit contact = contacts[closest];
        Vector3 point = contact.point;
        Vector3 normal = contact.normal;
        if (contact.distance <= 0f)
            GetTriggerContact(contact.collider, out point, out normal);
        Hit(contact.collider, point, normal);
    }

    bool CanCollide(Collider other)
    {
        if (other == null || other.transform.IsChildOf(projectile.transform))
            return false;
        if (projectile.Owner != null && other.transform.IsChildOf(projectile.Owner))
            return false;
        if (other.isTrigger)
            return _attackSystem.CanAffectTarget(other);

        Transform root = other.transform.root;
        return !root.CompareTag("Enemy") || root.GetComponentInChildren<LifeSystem>()?.IsDead != true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (finished || !CanCollide(other))
            return;
        GetTriggerContact(other, out Vector3 point, out Vector3 normal);
        Hit(other, point, normal);
    }

    void GetTriggerContact(Collider other, out Vector3 point, out Vector3 normal)
    {
        Vector3 center = hitCollider.transform.TransformPoint(hitCollider.center);
        Vector3 direction = projectile.transform.forward;
        float reach = hitCollider.bounds.extents.magnitude + speed * Time.fixedDeltaTime;
        if (other.Raycast(new Ray(center - direction * reach, direction), out RaycastHit contact, 2f * reach))
        {
            point = contact.point;
            normal = contact.normal;
            return;
        }

        point = other is MeshCollider mesh && !mesh.convex ? center : other.ClosestPoint(center);
        Vector3 separation = center - point;
        normal = separation.sqrMagnitude > 0.0001f ? separation.normalized : -direction;
    }

    void Hit(Collider other, Vector3 point, Vector3 normal)
    {
        impactPoint = point;
        impactNormal = normal;
        hitbox.DetectContact(other, point);
        if (!finished)
            Finish(other);
    }

    void OnHit(Collider other)
    {
        if (impactNormal == Vector3.zero)
            GetTriggerContact(other, out impactPoint, out impactNormal);
        Finish(other);
    }

    void Finish(Collider surface = null)
    {
        if (finished)
            return;
        finished = true;
        _moveSystem.Direction = Vector3.zero;
        _moveSystem.enabled = false;
        _attackSystem.StopAttack();
        if (surface != null && feedback != null)
            feedback.Hit(surface.transform, impactPoint, impactNormal);
        Destroy(projectile.gameObject);
    }
}

