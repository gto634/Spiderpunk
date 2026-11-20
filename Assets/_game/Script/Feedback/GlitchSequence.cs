using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
public sealed class GlitchSequence : MonoBehaviour
{
    [SerializeField] Texture2D frame1;
    [SerializeField] Texture2D frame2;
    [SerializeField] Texture2D frame3;
    [SerializeField, Min(0.01f), Tooltip("Total playback time for the three frames, in seconds.")]
    float duration = 0.2f;

    static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    MeshRenderer plane;
    MaterialPropertyBlock properties;
    double startedAt;
    int currentFrame = -1;
    Action onMidpoint;

    MeshRenderer Plane => plane ??= GetComponent<MeshRenderer>();

    void Awake()
    {
        if (currentFrame < 0)
            Plane.enabled = false;
    }

    [ContextMenu("Play")]
    public void Play() => TryPlay(null);

    public bool TryPlay(Action midpoint)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || currentFrame >= 0)
            return false;

        onMidpoint = midpoint;
        startedAt = Time.timeAsDouble;
        ShowFrame(0);
        return true;
    }

    void Update()
    {
        if (currentFrame < 0)
            return;

        double elapsed = Time.timeAsDouble - startedAt;
        if (elapsed >= duration && onMidpoint == null)
        {
            Hide();
            return;
        }

        int frame = duration > 0 ? Math.Min(2, (int)(elapsed * 3 / duration)) : 2;
        if (frame != currentFrame)
            ShowFrame(frame);

        if (onMidpoint != null && elapsed >= duration * 0.5)
        {
            Action callback = onMidpoint;
            onMidpoint = null;
            callback();
        }
    }

    void ShowFrame(int index)
    {
        currentFrame = index;
        Texture2D texture = index switch { 0 => frame1, 1 => frame2, _ => frame3 };
        Plane.enabled = texture != null;
        if (texture == null)
            return;

        properties ??= new MaterialPropertyBlock();
        plane.GetPropertyBlock(properties);
        properties.SetTexture(BaseMap, texture);
        plane.SetPropertyBlock(properties);
    }

    void OnDisable() => Hide();

    void Hide()
    {
        onMidpoint = null;
        currentFrame = -1;
        if (plane != null)
            plane.enabled = false;
    }
}
