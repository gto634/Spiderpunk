using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
internal class Attack
{
    [SerializeField] private AttackSystem attackSystem;
    [SerializeField] private Clock delayClock = new Clock();
    [SerializeField, Tooltip("Set the duration to zero to end the combo on this attack.")]
    private Clock durationClock = new Clock();
    [SerializeField] private Clock cooldownClock = new Clock();

    public AttackSystem AttackSystem => attackSystem;
    public Clock DelayClock => delayClock;
    public Clock DurationClock => durationClock;
    public Clock CooldownClock => cooldownClock;

    public void ResetClocks()
    {
        delayClock.ResetTimer();
        durationClock.ResetTimer();
        cooldownClock.ResetTimer();
    }
}

public class NormalAttackPlayer : MonoBehaviour
{
    [SerializeField] private List<Attack> attacks = new List<Attack>();

    private Attack activeAttack;
    private int activeAttackIndex;

    public bool IsAttacking => activeAttack != null;
    public int CurrentAttackIndex { get; private set; }
    internal int ActiveAttackIndex => activeAttackIndex;
    public event Action OnAttackFinished;
    public event Action<int> OnAttackChanged;

    public void TryAttack()
    {
        if (!isActiveAndEnabled || (activeAttack != null && !activeAttack.DurationClock.IsRunning))
            return;

        int nextIndex = activeAttack == null ? 0 : CurrentAttackIndex + 1;
        if (nextIndex >= attacks.Count)
            return;

        Attack next = attacks[nextIndex];
        if (next?.AttackSystem == null || !next.AttackSystem.CanAttack)
            return;

        if (activeAttack != null)
        {
            activeAttack.ResetClocks();
            if (activeAttack.AttackSystem != null)
                activeAttack.AttackSystem.StopAttack();

            if (!isActiveAndEnabled)
                return;
        }

        activeAttack = next;
        activeAttackIndex = nextIndex;
        CurrentAttackIndex = nextIndex;
        next.ResetClocks();
        next.DelayClock.StartTimer();
        next.AttackSystem.Attack();
        OnAttackChanged?.Invoke(nextIndex);

        AdvanceClocks(0d);
    }

    private void Update()
    {
        AdvanceClocks(Time.deltaTime);
    }

    private void AdvanceClocks(double deltaTime)
    {
        Attack attack = activeAttack;
        if (attack == null)
            return;

        if (!UpdatePhaseClocks(attack, deltaTime, out _))
            return;

        activeAttack = null;
        OnAttackFinished?.Invoke();
    }

    private bool UpdatePhaseClocks(Attack attack, double deltaTime, out double overflow)
    {
        overflow = 0d;
        if (attack.DelayClock.IsRunning)
        {
            if (!attack.DelayClock.UpdateTimer(deltaTime, out double delayOverflow))
                return false;

            deltaTime = delayOverflow;
            attack.DurationClock.StartTimer();
        }

        if (attack.DurationClock.IsRunning)
        {
            if (!attack.DurationClock.UpdateTimer(deltaTime, out double durationOverflow))
                return false;

            deltaTime = durationOverflow;
            CurrentAttackIndex = 0;
            attack.CooldownClock.StartTimer();
        }

        return attack.CooldownClock.UpdateTimer(deltaTime, out overflow);
    }

    private void OnDisable()
    {
        Attack attack = activeAttack;
        activeAttack = null;
        CurrentAttackIndex = 0;
        if (attack == null)
            return;

        attack.ResetClocks();
        if (attack.AttackSystem != null)
            attack.AttackSystem.StopAttack();
        OnAttackFinished?.Invoke();
    }
}
