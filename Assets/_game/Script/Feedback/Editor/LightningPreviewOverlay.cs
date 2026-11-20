using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), "spider-punk-lightning-preview", "Prévisualisation éclair", true)]
sealed class LightningPreviewOverlay : Overlay
{
    const string LightningPrefabGuid = "56f83a3de30246218396dd7054e254e5";

    public override void OnCreated()
    {
        Selection.selectionChanged += UpdateVisibility;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += UpdateVisibility;
    }

    public override void OnWillBeDestroyed()
    {
        Selection.selectionChanged -= UpdateVisibility;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.delayCall -= UpdateVisibility;
    }

    public override VisualElement CreatePanelContent()
    {
        var button = new Button(TogglePlayback)
        {
            text = PlaybackLabel(),
            tooltip = "Mettre les éclairs sélectionnés en pause ou reprendre leur prévisualisation."
        };
        button.schedule.Execute(() => button.text = PlaybackLabel()).Every(100);
        return button;
    }

    void UpdateVisibility()
    {
        displayed = SelectedLightning() != null;
    }

    void OnPlayModeChanged(PlayModeStateChange _) => UpdateVisibility();

    static string PlaybackLabel()
    {
        var lightning = SelectedLightning();
        if (lightning == null)
            return "Lecture";
        if (lightning.isPlaying)
            return "Pause";
        return lightning.isPaused ? "Reprendre" : "Lecture";
    }

    static void TogglePlayback()
    {
        var lightning = SelectedLightning();
        if (lightning == null)
            return;

        if (lightning.isPlaying)
            lightning.Pause(true);
        else
            lightning.Play(true);

        SceneView.RepaintAll();
    }

    static ParticleSystem SelectedLightning()
    {
        var selection = Selection.activeGameObject;
        if (EditorApplication.isPlayingOrWillChangePlaymode || selection == null ||
            EditorUtility.IsPersistent(selection) || !selection.activeInHierarchy ||
            !selection.TryGetComponent(out ParticleSystem lightning))
            return null;

        if (selection.TryGetComponent<LightningBurst>(out _))
            return lightning;

        var prefabStage = PrefabStageUtility.GetPrefabStage(selection);
        string assetPath = prefabStage != null && prefabStage.prefabContentsRoot == selection
            ? prefabStage.assetPath
            : PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selection);
        if (string.IsNullOrEmpty(assetPath))
            return null;

        return AssetDatabase.AssetPathToGUID(assetPath) == LightningPrefabGuid ? lightning : null;
    }
}

