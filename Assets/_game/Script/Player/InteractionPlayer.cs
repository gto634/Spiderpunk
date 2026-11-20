using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AttackSystem))]
public sealed class InteractionPlayer : MonoBehaviour
{
    AttackSystem attack;
    bool handled;

    void Awake() => attack = GetComponent<AttackSystem>();

    void OnEnable() => attack.OnHit += OnHit;

    void OnDisable()
    {
        attack.OnHit -= OnHit;
        attack.StopAttack();
    }

    public void Interact()
    {
        if (!isActiveAndEnabled || !attack.CanAttack || GameManager.Instance?.CanChangeEnemy != true)
            return;

        handled = false;
        attack.Attack(ignoreDelay: true);
    }

    void OnHit(Collider target)
    {
        if (handled || !target.CompareTag("Button"))
            return;

        handled = true;
        if (GameManager.Instance != null)
            GameManager.Instance.ChangeEnemy();
    }
}
