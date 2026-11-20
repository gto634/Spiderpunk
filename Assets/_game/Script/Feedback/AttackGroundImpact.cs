using UnityEngine;


[DefaultExecutionOrder(20)]
public sealed class AttackGroundImpact : MonoBehaviour
{
    [SerializeField] Transform guitarContact;
    [SerializeField] GameObject impactPrefab;
    [SerializeField] LayerMask groundLayers;
    [SerializeField] ScreenShakeSystem screenShakeSystem;
    [SerializeField] AttackSystem firstAttack;
    [SerializeField] AttackSystem secondAttack;
    [SerializeField, Min(0f)] float enemyHitShakeIntensity = 0.18f;
    [SerializeField, Min(0.01f)] float enemyHitShakeDuration = 0.16f;
    [SerializeField, Min(0f)] float screenShakeIntensity = 0.4f;
    [SerializeField, Min(0.01f)] float screenShakeDuration = 0.24f;

    const float ContactTolerance = 0.5f;
    bool impactPending;

    void OnEnable()
    {
        if (firstAttack != null)
            firstAttack.OnTargetHit += ShakeOnEnemyHit;
        if (secondAttack != null)
            secondAttack.OnTargetHit += ShakeOnEnemyHit;
    }

    void ShakeOnEnemyHit(GameObject target, Vector3 point)
    {
        screenShakeSystem.Play(point, enemyHitShakeIntensity, enemyHitShakeDuration);
    }

    public void QueueImpact()
    {
        impactPending = isActiveAndEnabled;
    }

    void LateUpdate()
    {
        if (!impactPending)
            return;

        impactPending = false;
        Vector3 origin = guitarContact.position + Vector3.up * ContactTolerance;
        screenShakeSystem.Play(guitarContact.position, screenShakeIntensity, screenShakeDuration);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2f * ContactTolerance,
                groundLayers, QueryTriggerInteraction.Ignore))
        {
            Quaternion rotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            Instantiate(impactPrefab, hit.point, rotation);
        }
    }

    void OnDisable()
    {
        if (firstAttack != null)
            firstAttack.OnTargetHit -= ShakeOnEnemyHit;
        if (secondAttack != null)
            secondAttack.OnTargetHit -= ShakeOnEnemyHit;
        impactPending = false;
    }
}
