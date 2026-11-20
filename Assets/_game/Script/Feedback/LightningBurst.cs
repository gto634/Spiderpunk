using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public sealed class LightningBurst : MonoBehaviour
{
    [SerializeField] LightningRibbon[] lightnings = System.Array.Empty<LightningRibbon>();

    public bool IsPlaying
    {
        get
        {
            foreach (var lightning in lightnings)
                if (lightning != null && lightning.IsAlive)
                    return true;

            return false;
        }
    }

    public void Play()
    {
        var group = GetComponent<ParticleSystem>();
        group.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        foreach (var lightning in lightnings)
            if (lightning != null)
                lightning.CaptureLaunchPose(Random.Range(0f, 360f));

        group.Play(true);
    }
}
