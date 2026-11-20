using System;
using UnityEngine;

public class KnockbackSystem : MonoBehaviour
{
    [SerializeField] Rigidbody _rb;

    public Action<int, Vector2> OnKnockback;

    void Awake()
    {
        if (_rb == null) _rb = GetComponentInParent<Rigidbody>();
    }

    public void Knockback(int Amount, Vector3 direction)
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        _rb.AddForce(direction * Amount, ForceMode.Impulse);

        OnKnockback?.Invoke(Amount, direction);
    }
}
