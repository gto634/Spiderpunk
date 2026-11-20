using NaughtyAttributes.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(Clock))]
public class ClockEditor : PropertyDrawer
{
    private const string StyleGuid = "3f0049a3db12637498396736aa48ed81";
    private const string FinishedTooltip = "Whether the timer reached its duration";
    private static readonly GUIContent FinishedToggle = new GUIContent(string.Empty, FinishedTooltip);

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var clockProperty = property.Copy();
        var row = new VisualElement();
        row.AddToClassList("clock-timer");

        var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(StyleGuid));
        if (style != null)
            row.styleSheets.Add(style);

        var label = new Label(property.displayName) { tooltip = property.tooltip };
        label.AddToClassList("clock-timer-label");

        bool multiple = property.serializedObject.isEditingMultipleObjects;
        var finished = new Toggle
        {
            tooltip = FinishedTooltip,
            showMixedValue = multiple
        };
        finished.SetEnabled(false);

        var currentTime = new DoubleField
        {
            formatString = "0.00",
            tooltip = "Elapsed time in seconds",
            showMixedValue = multiple
        };
        currentTime.AddToClassList("clock-timer-value");
        currentTime.SetEnabled(false);

        var duration = new PropertyField(property.FindPropertyRelative("duration"), "")
        {
            tooltip = "Duration in seconds"
        };
        duration.AddToClassList("clock-timer-value");

        row.Add(label);
        row.Add(finished);
        row.Add(currentTime);
        row.Add(new Label(" / "));
        row.Add(duration);

        if (!multiple)
        {
            void RefreshClock()
            {
                Clock clock = GetLiveClock(clockProperty);
                finished.SetValueWithoutNotify(clock?.IsFinished ?? false);
                currentTime.SetValueWithoutNotify(clock?.CurrentTime ?? 0d);
            }

            RefreshClock();
            row.schedule.Execute(RefreshClock).Every(50);
        }

        return row;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        label = EditorGUI.BeginProperty(position, label, property);
        position = EditorGUI.PrefixLabel(position, label);
        float finishedWidth = EditorGUIUtility.singleLineHeight + 2f;
        float width = Mathf.Max(0f, (position.width - finishedWidth - 12f) / 2f);
        var finishedRect = new Rect(position.x, position.y, finishedWidth, position.height);
        var elapsedRect = new Rect(finishedRect.xMax, position.y, width, position.height);
        var durationRect = new Rect(elapsedRect.xMax + 12f, position.y, width, position.height);

        bool multiple = property.serializedObject.isEditingMultipleObjects;
        Clock clock = multiple ? null : GetLiveClock(property);
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        using (new EditorGUI.DisabledScope(true))
        {
            bool previousMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = multiple;
            EditorGUI.ToggleLeft(finishedRect, FinishedToggle, clock?.IsFinished ?? false);
            EditorGUI.showMixedValue = previousMixedValue;

            string elapsed = multiple ? "—" : (clock?.CurrentTime ?? 0d).ToString("0.00");
            EditorGUI.TextField(elapsedRect, elapsed);
        }

        EditorGUI.LabelField(new Rect(elapsedRect.xMax, position.y, 12f, position.height), " / ");
        EditorGUI.PropertyField(durationRect, property.FindPropertyRelative("duration"), GUIContent.none);
        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }

    private static Clock GetLiveClock(SerializedProperty property) =>
        EditorApplication.isPlaying ? PropertyUtility.GetTargetObjectOfProperty(property) as Clock : null;
}


[CustomEditor(typeof(AttackSystem)), CanEditMultipleObjects]
internal sealed class AttackSystemEditor : NaughtyInspector
{
    public override bool RequiresConstantRepaint() => EditorApplication.isPlaying;
}

[CustomEditor(typeof(NormalAttackPlayer)), CanEditMultipleObjects]
internal sealed class NormalAttackPlayerEditor : NaughtyInspector
{
    public override bool RequiresConstantRepaint() => EditorApplication.isPlaying;
}
