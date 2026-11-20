using UnityEngine;

[DefaultExecutionOrder(20)]
public sealed class AttackCeilingWeb : MonoBehaviour
{
    [SerializeField] Transform leftHand;
    [SerializeField] WebLink webPrefab;
    [SerializeField] LayerMask ceilingLayers;
    [SerializeField, Min(0f)] float fadeDuration = 2f;
    [SerializeField] Material fadeMaterial;

    bool attachmentPending;
    WebLink activeWeb;

    public void QueueAttachment()
    {
        attachmentPending = isActiveAndEnabled;
    }

    void LateUpdate()
    {
        if (!attachmentPending)
            return;

        attachmentPending = false;
        if (activeWeb != null || !Physics.Raycast(leftHand.position, Vector3.up, out RaycastHit hit,
                Mathf.Infinity, ceilingLayers, QueryTriggerInteraction.Ignore))
            return;

        activeWeb = Instantiate(webPrefab, hit.point, Quaternion.identity);
        activeWeb.Attach(leftHand, hit.point);
    }

    public void Release()
    {
        attachmentPending = false;
        if (activeWeb != null)
        {
            activeWeb.FadeOut(fadeDuration, fadeMaterial);
            activeWeb = null;
        }
    }

    void OnDisable() => Release();
}
