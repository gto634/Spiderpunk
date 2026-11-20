using UnityEngine;

public sealed class SpiderSensePlayerFeedback : MonoBehaviour
{
    [SerializeField] AttackAimAssistPlayer aimAssist;
    [SerializeField] ShootSystem shootSystem;

    void OnEnable()
    {
        if (aimAssist != null)
            aimAssist.OnEnemyDetected += PlaySpiderSense;
        if (shootSystem != null)
            shootSystem.OnEnemyDetected += PlaySpiderSense;
    }

    void OnDisable()
    {
        if (aimAssist != null)
            aimAssist.OnEnemyDetected -= PlaySpiderSense;
        if (shootSystem != null)
            shootSystem.OnEnemyDetected -= PlaySpiderSense;
    }

    static void PlaySpiderSense(Transform enemy)
    {
        if (enemy == null)
            return;
        SpiderSense sense = enemy.GetComponentInChildren<SpiderSense>();
        if (sense != null)
            sense.Play();
    }
}
