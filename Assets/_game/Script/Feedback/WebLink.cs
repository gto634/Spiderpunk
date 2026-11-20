using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(30)]
public class WebLink : MonoBehaviour
{
    public Transform start, end, link;
    public float segmentLength = 1f;
    public float thickness = 1f;
    public int maxSegments = 200;

    readonly List<Transform> segmentList = new List<Transform>();
    Material fadingMaterial;
    Color fadeColor;
    float fadeStartTime;
    float fadeDuration;

    void OnDestroy()
    {
        Clear();
        if (fadingMaterial != null)
            Destroy(fadingMaterial);
    }

    void OnDisable() => Clear();

    public void Attach(Transform startAnchor, Vector3 endPoint)
    {
        start = startAnchor;
        end.position = endPoint;
        UpdateSegments();
    }

    public void FadeOut(float duration, Material material)
    {
        if (fadingMaterial != null)
            return;
        if (duration <= 0f || material == null || segmentList.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        start = null;
        fadeDuration = duration;
        fadeStartTime = Time.time;
        fadingMaterial = new Material(material);
        fadeColor = fadingMaterial.color;

        Material[] slots = link.GetComponent<MeshRenderer>().sharedMaterials;
        System.Array.Fill(slots, fadingMaterial);
        foreach (Transform segment in segmentList)
            segment.GetComponent<MeshRenderer>().sharedMaterials = slots;

        Destroy(gameObject, duration);
    }

    void LateUpdate()
    {
        if (fadingMaterial == null)
        {
            UpdateSegments();
            return;
        }

        float progress = Mathf.Clamp01((Time.time - fadeStartTime) / fadeDuration);
        Color color = fadeColor;
        color.a *= 1f - progress;
        fadingMaterial.color = color;
    }

    void UpdateSegments()
    {
        if (!start || !end || !link) return;

        var mf = link.GetComponent<MeshFilter>();
        var mr = link.GetComponent<MeshRenderer>();
        if (!mf || !mr) return;
        mr.enabled = false;

        Vector3 dir = end.position - start.position;
        float dist = dir.magnitude;
        if (dist < 0.0001f) return;

        int count = Mathf.Clamp(Mathf.CeilToInt(dist / Mathf.Max(segmentLength, 0.001f)), 1, maxSegments);
        float step = dist / count;
        Vector3 fwd = dir / dist;
        Quaternion rot = Quaternion.LookRotation(dir);

        while (segmentList.Count < count) segmentList.Add(CreateSegment(mf, mr));

        for (int i = 0; i < segmentList.Count; i++)
        {
            Transform t = segmentList[i];
            bool on = i < count;
            if (t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
            if (!on) continue;

            t.SetPositionAndRotation(start.position + fwd * (step * i), rot);
            t.localScale = new Vector3(thickness, thickness, step);
        }
    }

    Transform CreateSegment(MeshFilter mf, MeshRenderer mr)
    {
        var go = new GameObject("Segment", typeof(MeshFilter), typeof(MeshRenderer));
        go.hideFlags = HideFlags.HideAndDontSave;
        go.transform.SetParent(transform, false);
        go.GetComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
        go.GetComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials;
        return go.transform;
    }

    void Clear()
    {
        foreach (var t in segmentList)
            if (t) { if (Application.isPlaying) Destroy(t.gameObject); else DestroyImmediate(t.gameObject); }
        segmentList.Clear();
    }
}
