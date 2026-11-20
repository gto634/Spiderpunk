using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class WebImpactDecal : MonoBehaviour
{
    [Serializable]
    struct Variation
    {
        public Material material;
        [Min(0.01f)] public float width;
    }

    [SerializeField] DecalProjector projector;
    [SerializeField] Variation[] variations;
    [SerializeField] Clock lifeClock = new Clock();

    public void Place(Transform surface, Vector3 point, Vector3 normal)
    {
        if (projector == null || variations == null || variations.Length == 0)
            return;

        Variation variation = variations[UnityEngine.Random.Range(0, variations.Length)];
        if (variation.material == null)
            return;

        transform.SetParent(surface, false);
        transform.SetPositionAndRotation(point, Quaternion.FromToRotation(Vector3.forward, -normal));
        projector.size = new Vector3(variation.width, variation.width, projector.size.z);
        projector.material = variation.material;
        lifeClock.StartTimer();
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (lifeClock.UpdateTimer(out _))
        {
            Destroy(gameObject);
            return;
        }
        projector.fadeFactor = 1f - (float)(lifeClock.CurrentTime / lifeClock.Duration);
    }
}
