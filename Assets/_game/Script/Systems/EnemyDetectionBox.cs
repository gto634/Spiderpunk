using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class EnemyDetectionBox
{
    readonly List<Transform> enemies = new List<Transform>();
    Collider[] overlaps = new Collider[16];

    public IReadOnlyList<Transform> Query(BoxCollider hitbox, Transform owner)
    {
        Transform area = hitbox.transform;
        return Query(hitbox, owner, area.TransformPoint(hitbox.center), area.rotation);
    }

    public IReadOnlyList<Transform> Query(BoxCollider hitbox, Transform owner, Vector3 center, Quaternion rotation)
    {
        enemies.Clear();
        if (!hitbox.gameObject.activeInHierarchy)
            return enemies;

        Vector3 scale = hitbox.transform.lossyScale;
        scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        Vector3 halfExtents = Vector3.Scale(hitbox.size * 0.5f, scale);

        Physics.SyncTransforms();
        int count;
        while (true)
        {
            count = Physics.OverlapBoxNonAlloc(center, halfExtents, overlaps, rotation,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            if (count < overlaps.Length)
                break;
            Array.Resize(ref overlaps, overlaps.Length * 2);
        }

        for (int i = 0; i < count; i++)
        {
            Transform contact = overlaps[i].transform;
            if (owner != null && contact.IsChildOf(owner))
                continue;

            Transform enemy = null;
            for (Transform parent = contact; parent != null; parent = parent.parent)
                if (parent.CompareTag("Enemy"))
                    enemy = parent;

            if (enemy != null && !enemies.Contains(enemy) &&
                enemy.GetComponentInChildren<LifeSystem>()?.IsDead != true)
                enemies.Add(enemy);
        }
        return enemies;
    }
}
