using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RotateSystem))]
public class AttackAimAssistPlayer : MonoBehaviour
{
    [SerializeField] RotateSystem rotateSystem;
    [SerializeField] NormalAttackPlayer normalAttackPlayer;
    [SerializeField] BrainPlayer brainPlayer;
    [SerializeField] BoxCollider detectionHitbox;

    readonly EnemyDetectionBox enemyDetection = new EnemyDetectionBox();
    public event Action<Transform> OnEnemyDetected;

    void Reset()
    {
        rotateSystem = GetComponent<RotateSystem>();
        normalAttackPlayer = transform.root.GetComponentInChildren<NormalAttackPlayer>();
        brainPlayer = transform.root.GetComponentInChildren<BrainPlayer>();
    }

    void Awake()
    {
        if (rotateSystem != null && normalAttackPlayer != null && brainPlayer != null && detectionHitbox != null)
            return;

        Debug.LogError("AttackAimAssistPlayer: assign Rotate System, Normal Attack Player, Brain Player and Detection Hitbox.", this);
        enabled = false;
    }

    void OnEnable()
    {
        normalAttackPlayer.OnAttackChanged += FaceClosestEnemy;
    }

    void OnDisable()
    {
        if (normalAttackPlayer != null)
            normalAttackPlayer.OnAttackChanged -= FaceClosestEnemy;
    }

    void FaceClosestEnemy(int _)
    {
        if (!brainPlayer.isActiveAndEnabled || brainPlayer.IsAiming || !rotateSystem.isActiveAndEnabled ||
            rotateSystem.Target == null || !detectionHitbox.enabled || !detectionHitbox.gameObject.activeInHierarchy)
            return;

        Transform owner = detectionHitbox.attachedRigidbody != null
            ? detectionHitbox.attachedRigidbody.transform : rotateSystem.Target;
        IReadOnlyList<Transform> enemies = enemyDetection.Query(detectionHitbox, owner);
        if (enemies.Count == 0)
            return;

        Transform target = enemies[0];
        Vector3 origin = rotateSystem.Target.position;
        float nearestDistance = float.PositiveInfinity;
        Vector3 nearestDirection = Vector3.zero;
        for (int i = 0; i < enemies.Count; i++)
        {
            Transform enemy = enemies[i];
            Vector3 offset = enemy.position - origin;
            Vector3 direction = Vector3.ProjectOnPlane(offset, Vector3.up);
            if (direction.sqrMagnitude < 0.0001f || offset.sqrMagnitude >= nearestDistance)
                continue;

            nearestDistance = offset.sqrMagnitude;
            nearestDirection = direction;
            target = enemy;
        }

        if (nearestDirection != Vector3.zero)
            rotateSystem.RotateTowards(nearestDirection);
        OnEnemyDetected?.Invoke(target);
    }
}
