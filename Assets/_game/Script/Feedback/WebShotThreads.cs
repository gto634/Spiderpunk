using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class WebShotThreads : MonoBehaviour
{
    public void Play() => Play(transform.parent != null ? transform.parent : transform);

    public void Play(Transform shootPoint)
    {
        Vector3 heading = Vector3.ProjectOnPlane(shootPoint.forward, Vector3.up);
        if (heading.sqrMagnitude < 0.0001f)
            heading = Vector3.Cross(shootPoint.right, Vector3.up);
        transform.SetPositionAndRotation(shootPoint.position, Quaternion.LookRotation(heading));

        var group = GetComponent<ParticleSystem>();
        group.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        group.Play(true);
    }

    public void Stop() => GetComponent<ParticleSystem>().Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
}
