using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public sealed class ScenePortal : MonoBehaviour
{
    [SerializeField] string destination;

    void OnTriggerEnter(Collider other)
    {
        var body = other.attachedRigidbody;
        if (!other.isTrigger && body != null && body.CompareTag("Player") && GameManager.Instance != null)
            GameManager.Instance.TravelTo(destination);
    }
}
