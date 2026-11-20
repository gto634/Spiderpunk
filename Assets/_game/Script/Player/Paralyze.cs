using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AttackSystem))]
public sealed class Paralyze : MonoBehaviour
{
    AttackSystem attackSystem;

    void Awake() => attackSystem = GetComponent<AttackSystem>();

    void OnEnable() => attackSystem.OnTargetHit += ShakeTarget;

    void OnDisable()
    {
        if (attackSystem != null)
            attackSystem.OnTargetHit -= ShakeTarget;
    }

    static void ShakeTarget(GameObject target, Vector3 _)
    {
        if (target != null && target.GetComponentInChildren<ShakeSystem>() is { } shake)
            shake.Play();
    }
}
