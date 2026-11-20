using System;
using System.Collections.Generic;
using UnityEngine;

public enum KnockbackType
{
    AwayFromAttacker,
    AttackerForward
}

[System.Serializable]
public class HitboxSettings
{
    public AttackHitbox hitbox;
    
    [SerializeField] private Clock _delayClock = new Clock();
    [SerializeField] private Clock _durationClock = new Clock();

    internal Clock DelayClock => _delayClock;
    internal Clock DurationClock => _durationClock;

    public float Delay => (float)_delayClock.Duration;
    public float Duration { get => (float)_durationClock.Duration; set => _durationClock.Duration = value; }

    [SerializeField, Min(0)] int _damage;
    [SerializeField] int _knockback;
    [SerializeField] KnockbackType _knockbackType;

    public int Damage { get => _damage; set => _damage = value; }
    public int Knockback { get => _knockback; set => _knockback = value; }
    public KnockbackType KnockbackType  => _knockbackType;
}

public class AttackSystem : MonoBehaviour
{
    [SerializeField] List<HitboxSettings> _hitboxes = new List<HitboxSettings>();
    [SerializeField] List<string> _damageableTags = new List<string>();
    [SerializeField] List<string> _knockbackableTags = new List<string>();

    public List<HitboxSettings> Hitboxes => _hitboxes;

    private bool _isInitialized;

    public bool IsAttacking { get; private set; }
    public bool CanAttack => _isInitialized && isActiveAndEnabled
        && !IsAttacking;

    public event Action Activated;
    public event Action Desactivated;
    public event Action<Collider> OnHit;
    public event Action<GameObject, Vector3> OnTargetHit;

    private void Awake()
    {
        if (_hitboxes == null || _hitboxes.Count == 0)
        {
            Debug.LogError("AttackSystem : configure au moins une hitbox.", this);
            enabled = false;
            return;
        }

        foreach (HitboxSettings settings in _hitboxes)
        {
            if (settings == null || settings.hitbox == null
                || settings.hitbox.Collider == null)
            {
                Debug.LogError("AttackSystem : chaque hitbox doit avoir une référence et un Collider.", this);
                enabled = false;
                return;
            }
        }

        foreach (HitboxSettings settings in _hitboxes)
        {
            ResetHitbox(settings);
        }

        _isInitialized = true;
    }


    private void OnEnable()
    {
        if (!_isInitialized) return;

        foreach (HitboxSettings settings in _hitboxes)
        {
            if (settings.hitbox != null)
            {
                settings.hitbox.ContactDetected += HandleContact;
            }
        }
    }

    private void Update()
    {
        if (!IsAttacking) return;

        bool hasPendingHitbox = false;

        foreach (HitboxSettings settings in _hitboxes)
        {
            if (!settings.DelayClock.IsRunning && !settings.DurationClock.IsRunning) continue;

            if (settings.hitbox == null || settings.hitbox.Collider == null)
            {
                ResetHitbox(settings);
                continue;
            }

            double deltaTime = Time.deltaTime;

            if (settings.DelayClock.IsRunning)
            {
                if (!settings.DelayClock.UpdateTimer(deltaTime, out double overflowTime))
                {
                    hasPendingHitbox = true;
                    continue;
                }

                deltaTime = overflowTime;

                settings.DurationClock.StartTimer();
                settings.hitbox.Collider.enabled = true;
                Activated?.Invoke();

                if (!isActiveAndEnabled || !IsAttacking) return;
            }

            if (settings.DurationClock.UpdateTimer(deltaTime, out _))
            {
                settings.hitbox.Collider.enabled = false;
                Desactivated?.Invoke();

                if (!isActiveAndEnabled || !IsAttacking) return;
            }
            else
            {
                hasPendingHitbox = true;
            }
        }

        IsAttacking = hasPendingHitbox;
    }

    private void OnDisable()
    {
        if (!_isInitialized) return;

        IsAttacking = false;

        foreach (HitboxSettings settings in _hitboxes)
        {
            if (settings.hitbox != null)
            {
                settings.hitbox.ContactDetected -= HandleContact;
            }

            bool wasActive = settings.DurationClock.IsRunning;
            ResetHitbox(settings);

            if (wasActive)
            {
                Desactivated?.Invoke();
            }
        }
    }

    private static void ResetHitbox(HitboxSettings settings)
    {
        settings.DelayClock.ResetTimer();
        settings.DurationClock.ResetTimer();

        if (settings.hitbox != null && settings.hitbox.Collider != null)
        {
            settings.hitbox.Collider.enabled = false;
        }
    }

    private void HandleContact(Collider source, Collider target, Vector3? sweptHitPoint)
    {
        GameObject root = target.transform.root.gameObject;
        GameObject sourceRoot = source.transform.root.gameObject;

        Projectile projectile = sourceRoot.GetComponent<Projectile>();

        GameObject attacker = projectile != null && projectile.Owner != null
            ? projectile.Owner.root.gameObject : sourceRoot;

        if (attacker == root) return;

        CaptiveSpring captive = GetCaptiveTarget(target);
        bool canDamage = _damageableTags.Contains(target.tag) ||
            (projectile != null && _damageableTags.Contains(root.tag));
        bool canKnockback = _knockbackableTags.Contains(target.tag) ||
            (projectile != null && _knockbackableTags.Contains(root.tag));

        if (captive == null && !canDamage && !canKnockback) return;

        LifeSystem lifeSystem = captive != null ? null : root.GetComponentInChildren<LifeSystem>();
        if (lifeSystem?.IsDead == true) return;

        HitboxSettings settings = _hitboxes.Find(candidate => candidate.hitbox.Collider == source);
        if (settings == null) return;
        if (!settings.hitbox.TryRegisterHit(target)) return;

        if (captive != null)
        {
            Vector3 direction = projectile != null ? projectile.transform.forward :
                Vector3.ProjectOnPlane(target.bounds.center - attacker.transform.position, Vector3.up);
            if (direction.sqrMagnitude < .0001f)
                direction = source.transform.forward;
            captive.Recoil(direction, settings.Knockback);
            OnHit?.Invoke(target);
            return;
        }

        Debug.Log($"{target.name}_{root.name} est touché par {source.name}_{sourceRoot.name}");
        OnTargetHit?.Invoke(root, AttackHitbox.GetImpactPoint(source, target, sweptHitPoint));
        OnHit?.Invoke(target);

        if (canDamage && lifeSystem != null && settings.Damage > 0)
            lifeSystem.TakeDamage(settings.Damage);

        if (canKnockback && lifeSystem?.IsDead != true)
        {
            KnockbackSystem knockbackSystem = root.GetComponentInChildren<KnockbackSystem>();
            if (knockbackSystem != null && settings.KnockbackType == KnockbackType.AttackerForward)
            {
                knockbackSystem.Knockback(settings.Knockback, source.transform.forward);
            }
            else if (knockbackSystem != null && settings.KnockbackType == KnockbackType.AwayFromAttacker)
            {
                Vector3 knockbackDirection = root.transform.position - source.transform.root.position;
                knockbackSystem.Knockback(settings.Knockback, new Vector3(knockbackDirection.x, 0, knockbackDirection.z).normalized);
            }
        }
    }

    CaptiveSpring GetCaptiveTarget(Collider target) =>
        _knockbackableTags.Contains("Enemy") && target.TryGetComponent(out CaptiveSpring captive) &&
        captive.isActiveAndEnabled ? captive : null;

    public bool CanAffectTarget(Collider target)
    {
        if (target == null) return false;
        if (GetCaptiveTarget(target) != null) return true;
        Transform root = target.transform.root;
        string rootTag = root.tag;
        bool hasTargetTag = _damageableTags.Contains(target.tag) || _damageableTags.Contains(rootTag) ||
            _knockbackableTags.Contains(target.tag) || _knockbackableTags.Contains(rootTag);
        return hasTargetTag && root.GetComponentInChildren<LifeSystem>()?.IsDead != true;
    }

    public void Attack()
    {
        Attack(ignoreDelay: false);
    }

    public void Attack(bool ignoreDelay)
    {
        if (!CanAttack) return;

        IsAttacking = true;

        foreach (HitboxSettings settings in _hitboxes)
        {
            ResetHitbox(settings);
            settings.hitbox.ResetHits();
            if (!ignoreDelay)
                settings.DelayClock.StartTimer();
        }

        if (!ignoreDelay) return;

        foreach (HitboxSettings settings in _hitboxes)
        {
            settings.DurationClock.StartTimer();
            settings.hitbox.Collider.enabled = true;
            Activated?.Invoke();

            if (!isActiveAndEnabled || !IsAttacking) return;
        }
    }

    public void StopAttack()
    {
        if (!_isInitialized) return;

        IsAttacking = false;

        foreach (HitboxSettings settings in _hitboxes)
        {
            bool wasActive = settings.DurationClock.IsRunning;
            ResetHitbox(settings);

            if (wasActive)
            {
                Desactivated?.Invoke();
            }
        }
    }
}
