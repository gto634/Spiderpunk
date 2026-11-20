using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(WebShotThreads))]
sealed class WebShotThreadsEditor : Editor
{
    static ParticleSystem previewGroup;

    static WebShotThreadsEditor()
    {
        Selection.selectionChanged += () =>
        {
            if (previewGroup && Selection.activeGameObject != previewGroup.gameObject)
                ClearInspectorPreview();
        };
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                ClearInspectorPreview();
        };
        AssemblyReloadEvents.beforeAssemblyReload += ClearInspectorPreview;
    }

    public override VisualElement CreateInspectorGUI()
    {
        var inspector = new VisualElement();
        InspectorElement.FillDefaultInspector(inspector, serializedObject, this);
        var controls = new VisualElement();
        controls.style.flexDirection = FlexDirection.Row;
        var play = new Button(Play) { name = "play-shot", text = "Play" };
        var stop = new Button(Stop) { name = "stop-shot", text = "Stop" };
        play.style.flexGrow = stop.style.flexGrow = 1;
        controls.Add(play);
        controls.Add(stop);
        inspector.Add(controls);
        return inspector;
    }

    void Play()
    {
        var shot = (WebShotThreads)target;
        if (EditorUtility.IsPersistent(shot))
        {
            var stage = PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(shot));
            if (stage == null || !stage.prefabContentsRoot.TryGetComponent(out shot))
                return;
            Selection.activeGameObject = shot.gameObject;
        }

        if (Application.IsPlaying(shot))
            shot.Play();
        else
        {
            ClearInspectorPreview();
            previewGroup = shot.GetComponent<ParticleSystem>();
            previewGroup.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            previewGroup.Play(true);
        }

        SceneView.RepaintAll();
    }

    void Stop()
    {
        var shot = (WebShotThreads)target;
        if (EditorUtility.IsPersistent(shot))
            return;

        shot.Stop();
        if (previewGroup && previewGroup.gameObject == shot.gameObject)
            previewGroup = null;
        SceneView.RepaintAll();
    }

    static void ClearInspectorPreview()
    {
        if (previewGroup)
            previewGroup.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        previewGroup = null;
    }
}
