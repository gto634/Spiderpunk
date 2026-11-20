using UnityEngine;

public class ShootAttackPlayer : MonoBehaviour
{
    [SerializeField] ShootSystem _shootSystem;
    [SerializeField] Clock cooldownClock = new Clock();

    public bool CanAttack => isActiveAndEnabled && _shootSystem != null &&
        _shootSystem.CanShoot && !cooldownClock.IsRunning;

    public bool StartAttack() => StartAttack(false);

    public bool StartAttack(bool fireImmediately)
    {
        if (!CanAttack)
            return false;

        cooldownClock.StartTimer();
        if (fireImmediately)
            _shootSystem.ShootImmediately();
        else
            _shootSystem.Shoot();
        return true;
    }

    public void CancelAttack()
    {
        if (_shootSystem != null)
            _shootSystem.CancelShoot();
    }

    void Update() => cooldownClock.UpdateTimer(out _);

    void OnDisable()
    {
        CancelAttack();
        cooldownClock.ResetTimer();
    }
}

