using UnityEngine;

public class WebProjectileFeedback : MonoBehaviour
{
    [SerializeField] Transform visual;
    [SerializeField] WebImpactDecal impactDecal;
    [SerializeField, Min(0f)] float maximumLength = 10f;
    [SerializeField, Min(0f)] float growthSpeed = 10f;

    void Update()
    {
        if (visual == null)
            return;
        Vector3 scale = visual.localScale;
        scale.z = Mathf.MoveTowards(scale.z, maximumLength, growthSpeed * Time.deltaTime);
        visual.localScale = scale;
    }

    public void Hit(Transform surface, Vector3 point, Vector3 normal)
    {
        if (impactDecal != null)
            impactDecal.Place(surface, point, normal);
    }
}

