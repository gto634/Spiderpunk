using System;
using UnityEngine;

public sealed class NormalAttackWoodChips : MonoBehaviour
{
    [SerializeField] AttackSystem[] attacks = Array.Empty<AttackSystem>();
    [SerializeField] ParticleSystem woodChips;

    void OnEnable()
    {
        if (woodChips == null)
        {
            Debug.LogError("NormalAttackWoodChips: assign the WoodChips prefab.", this);
            enabled = false;
            return;
        }

        foreach (var attack in attacks)
            if (attack != null)
                attack.OnTargetHit += SpawnWoodChips;
    }

    void OnDisable()
    {
        foreach (var attack in attacks)
            if (attack != null)
                attack.OnTargetHit -= SpawnWoodChips;
    }

    void SpawnWoodChips(GameObject _, Vector3 impactPoint)
    {
        Instantiate(woodChips, impactPoint, woodChips.transform.rotation);
    }
}
