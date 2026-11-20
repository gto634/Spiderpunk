using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(ParticleSystem))]
sealed class LightningRibbon : MonoBehaviour
{
    const int MaxPoints = 4;
    const int PathVariantCount = 3;
    const float TextureAspect = 367f / 544f;

    [SerializeField, Min(.01f), Tooltip("Length of one texture tile. Increase to zoom in.")]
    float textureTileLength = 1.2f;

    readonly ParticleSystem.Particle[] particle = new ParticleSystem.Particle[1];
    readonly Vector3[] path = new Vector3[MaxPoints];
    readonly float[] times = new float[MaxPoints];
    readonly Vector3[] points = new Vector3[MaxPoints];
    readonly float[] distances = new float[MaxPoints];
    readonly Vector3[] vertices = new Vector3[MaxPoints * 2];
    readonly Vector2[] uv = new Vector2[MaxPoints * 2];
    static readonly int[] Triangles = { 0, 2, 1, 1, 2, 3, 2, 4, 3, 3, 4, 5, 4, 6, 5, 5, 6, 7 };

    ParticleSystem particles;
    GameObject visual;
    Mesh mesh;
    int pathCount;
    Matrix4x4 launchToWorld;
    bool hasLaunchPose;

    public bool IsAlive => isActiveAndEnabled && particles != null && particles.IsAlive(false);

    void OnEnable()
    {
        if (!gameObject.scene.IsValid())
            return;

        particles = GetComponent<ParticleSystem>();
        hasLaunchPose = false;
        var material = GetComponent<ParticleSystemRenderer>().trailMaterial;
        visual = new GameObject("Lightning Ribbon Preview") { hideFlags = HideFlags.HideAndDontSave, layer = gameObject.layer };
        visual.transform.SetParent(transform, false);
        mesh = new Mesh { name = "Lightning Ribbon", hideFlags = HideFlags.HideAndDontSave };
        mesh.MarkDynamic();
        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = visual.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        ReadPath();
        RenderPipelineManager.beginCameraRendering += RenderRibbon;
    }

    public void CaptureLaunchPose(float roll)
    {
        if (!isActiveAndEnabled)
            return;

        launchToWorld = transform.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.AngleAxis(roll, Vector3.forward));
        hasLaunchPose = true;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= RenderRibbon;
        if (Application.IsPlaying(gameObject))
        {
            Destroy(visual);
            Destroy(mesh);
        }
        else
        {
            DestroyImmediate(visual);
            DestroyImmediate(mesh);
        }
    }

    void ReadPath()
    {
        var main = particles.main;
        var velocity = particles.velocityOverLifetime;
        var sideways = velocity.x;
        var speed = velocity.speedModifier;
        float stopTime = 1f;
        AnimationCurve speedCurve = speed.curve;
        if (speedCurve != null)
        {
            for (int i = 1; i < speedCurve.length; i++)
            {
                if (speedCurve[i].value > 0f)
                    continue;

                stopTime = (speedCurve[i - 1].time + speedCurve[i].time) * .5f;
                break;
            }
        }

        float lifetime = main.startLifetime.constant;
        float forwardSpeed = main.startSpeed.constant;
        float lateralSpeed = sideways.Evaluate(0f);
        pathCount = 1;
        path[0] = Vector3.zero;
        times[0] = 0f;
        AnimationCurve turns = sideways.curve;
        if (turns != null)
        {
            for (int i = 1; i < turns.length && pathCount < MaxPoints - 1; i++)
            {
                Keyframe from = turns[i - 1];
                Keyframe to = turns[i];
                float bendTime = (from.time + to.time) * .5f;
                if (bendTime >= stopTime || Mathf.Approximately(from.value, to.value))
                    continue;

                AddPoint(bendTime, new Vector3(lateralSpeed, 0f, forwardSpeed) * lifetime);
                lateralSpeed = to.value * sideways.curveMultiplier;
            }
        }

        AddPoint(stopTime, new Vector3(lateralSpeed, 0f, forwardSpeed) * lifetime);
    }

    void AddPoint(float time, Vector3 speed)
    {
        int previous = pathCount - 1;
        path[pathCount] = path[previous] + speed * (time - times[previous]);
        times[pathCount++] = time;
    }

    void RenderRibbon(ScriptableRenderContext _, Camera camera)
    {
        if (particles.GetParticles(particle) == 0 || particle[0].startLifetime <= 0f)
        {
            mesh.Clear();
            return;
        }

        if (!Application.IsPlaying(gameObject))
            ReadPath();

        float age = 1f - particle[0].remainingLifetime / particle[0].startLifetime;
        points[0] = path[0];
        int count = 1;
        for (int i = 1; i < pathCount && age > times[i - 1]; i++)
        {
            float progress = Mathf.InverseLerp(times[i - 1], times[i], age);
            points[count++] = Vector3.LerpUnclamped(path[i - 1], path[i], progress);
        }

        mesh.Clear();
        if (count < 2)
            return;

        distances[0] = 0f;
        for (int i = 1; i < count; i++)
            distances[i] = distances[i - 1] + Vector3.Distance(points[i - 1], points[i]);
        if (distances[count - 1] <= Mathf.Epsilon)
            return;

        var width = particles.trails.widthOverTrail;
        float tilesPerUnit = 1f / Mathf.Max(.01f, textureTileLength);

        Matrix4x4 ribbonToWorld = hasLaunchPose ? launchToWorld : transform.localToWorldMatrix;
        float holdProgress = Mathf.InverseLerp(times[pathCount - 1], 1f, age);
        int variant = Mathf.Min((int)(holdProgress * PathVariantCount), PathVariantCount - 1);
        if (variant > 0)
        {
            float roll = variant * (360f / PathVariantCount);
            ribbonToWorld *= Matrix4x4.Rotate(Quaternion.AngleAxis(roll, Vector3.forward));
        }

        Matrix4x4 worldToRibbon = ribbonToWorld.inverse;
        Matrix4x4 ribbonToMesh = transform.worldToLocalMatrix * ribbonToWorld;
        Vector3 cameraPosition = worldToRibbon.MultiplyPoint3x4(camera.transform.position);
        Vector3 cameraForward = worldToRibbon.MultiplyVector(camera.transform.forward);
        for (int i = 0; i < count; i++)
        {
            Vector3 view = camera.orthographic ? cameraForward : points[i] - cameraPosition;
            Vector3 incoming = Vector3.Cross(view, points[i] - points[Mathf.Max(0, i - 1)]).normalized;
            Vector3 outgoing = Vector3.Cross(view, points[Mathf.Min(count - 1, i + 1)] - points[i]).normalized;
            Vector3 joint = (incoming + outgoing).normalized;
            if (joint == Vector3.zero)
                joint = worldToRibbon.MultiplyVector(camera.transform.right).normalized;

            float progress = distances[i] / distances[count - 1];
            float halfWidth = width.Evaluate(1f - progress) * .5f;
            float alignment = Mathf.Max(Vector3.Dot(joint, incoming), Vector3.Dot(joint, outgoing));
            Vector3 edge = joint * (halfWidth / Mathf.Max(.45f, alignment));
            vertices[i * 2] = ribbonToMesh.MultiplyPoint3x4(points[i] + edge);
            vertices[i * 2 + 1] = ribbonToMesh.MultiplyPoint3x4(points[i] - edge);

            float u = 1f - distances[i] * tilesPerUnit;
            float v = halfWidth * tilesPerUnit * TextureAspect;
            uv[i * 2] = new Vector2(u, .5f - v);
            uv[i * 2 + 1] = new Vector2(u, .5f + v);
        }

        mesh.SetVertices(vertices, 0, count * 2);
        mesh.SetUVs(0, uv, 0, count * 2);
        mesh.SetIndices(Triangles, 0, (count - 1) * 6, MeshTopology.Triangles, 0);
    }
}

