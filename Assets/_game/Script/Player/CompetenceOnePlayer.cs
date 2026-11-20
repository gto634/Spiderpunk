using System;
using UnityEngine;

[RequireComponent(typeof(AttackSystem))]
public class CompetenceOnePlayer : MonoBehaviour
{
    [SerializeField] AttackSystem attackSystem;
    [SerializeField] Clock cooldownClock = new Clock();

    internal bool IsPlayingAnimation { get; set; }
    public bool IsAttacking => IsPlayingAnimation || (attackSystem != null && attackSystem.IsAttacking);
    public bool CanAttack => isActiveAndEnabled && attackSystem != null &&
        !IsAttacking && attackSystem.CanAttack && !cooldownClock.IsRunning;
    public event Action OnTriggered;

    void Reset() => attackSystem = GetComponent<AttackSystem>();

    public bool TryAttack()
    {
        if (!CanAttack)
            return false;

        cooldownClock.StartTimer();
        attackSystem.Attack();
        OnTriggered?.Invoke();
        return true;
    }

    void Update() => cooldownClock.UpdateTimer(out _);

    void OnDisable()
    {
        IsPlayingAnimation = false;
        if (attackSystem != null)
            attackSystem.StopAttack();
        cooldownClock.ResetTimer();
    }
}

