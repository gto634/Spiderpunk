using UnityEngine;

public class WebImpact : MonoBehaviour
{
    [SerializeField] Clock lifeClock = new Clock();
    Renderer[] renderers;
    MaterialPropertyBlock properties;
    static readonly int Melt = Shader.PropertyToID("_Melt");

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        properties = new MaterialPropertyBlock();
        lifeClock.StartTimer();
    }

    public void Init(Vector3 normal, Vector3 position)
    {
        transform.rotation = Quaternion.FromToRotation(Vector3.forward, normal);
        transform.position = position + normal * 0.01f;
    }

    void Update()
    {
        bool finished = lifeClock.UpdateTimer(out _);
        float progress = lifeClock.Duration > 0d ? (float)(lifeClock.CurrentTime / lifeClock.Duration) : 1f;
        foreach (Renderer visual in renderers)
        {
            if (visual == null || visual.sharedMaterial == null || !visual.sharedMaterial.HasProperty(Melt))
                continue;
            visual.GetPropertyBlock(properties);
            properties.SetFloat(Melt, progress);
            visual.SetPropertyBlock(properties);
        }

        if (finished)
            Destroy(gameObject);
    }
}

