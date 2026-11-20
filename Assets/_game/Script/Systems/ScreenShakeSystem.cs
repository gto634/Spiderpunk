using Unity.Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScreenShakeSystem : MonoBehaviour
{
    const int ScreenShakeChannel = 1 << 8;

    void Awake()
    {
        foreach (var camera in transform.root.GetComponentsInChildren<CinemachineCamera>(true))
        {
            var listener = camera.gameObject.AddComponent<CinemachineImpulseListener>();
            listener.ChannelMask = ScreenShakeChannel;
            listener.Gain = 1f;
            listener.UseCameraSpace = true;
            listener.ApplyAfter = CinemachineCore.Stage.Noise;
        }
    }

    public void Play(Vector3 position, float intensity, float duration)
    {
        if (!isActiveAndEnabled || intensity <= 0f || duration <= 0f)
            return;

        var impulse = new CinemachineImpulseDefinition
        {
            ImpulseChannel = ScreenShakeChannel,
            ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump,
            ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
            ImpulseDuration = duration
        };
        impulse.CreateEvent(position, Vector3.down * intensity);
    }
}
