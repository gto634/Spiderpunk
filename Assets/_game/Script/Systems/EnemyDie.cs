using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyDie : MonoBehaviour
{
    [SerializeField] LifeSystem lifeSystem;
    [SerializeField] GameObject capturedPrefab;

    public GameObject CapturedPrefab => capturedPrefab;

    void OnEnable() => lifeSystem.OnDeath += Die;
    void OnDisable() => lifeSystem.OnDeath -= Die;

    void Die()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Capture(this);
    }
}
