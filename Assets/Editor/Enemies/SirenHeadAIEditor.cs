using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(SirenHeadAI))]
public sealed class SirenHeadAIEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        InspectorElement.FillDefaultInspector(root, serializedObject, this);
        var state = new TextField("Current State");
        var lastSeen = new Vector3Field("Last Seen Position");
        var time = new FloatField("Search Time Remaining");
        var path = new TextField("Path Status");
        var ready = new Toggle("Encounter Ready");
        var readOnly = new VisualElement();
        readOnly.Add(new Label("Runtime Diagnostics (read only)"));
        readOnly.Add(state); readOnly.Add(lastSeen); readOnly.Add(time); readOnly.Add(path); readOnly.Add(ready);
        readOnly.SetEnabled(false);
        root.Add(readOnly);
        root.schedule.Execute(() =>
        {
            var enemy = target as SirenHeadAI;
            if (enemy == null) return;
            state.SetValueWithoutNotify(enemy.State.ToString());
            lastSeen.SetValueWithoutNotify(enemy.LastSeenPosition);
            time.SetValueWithoutNotify(enemy.SearchTimeRemaining);
            path.SetValueWithoutNotify(enemy.PathStatus.ToString());
            ready.SetValueWithoutNotify(enemy.EncounterReady);
        }).Every(100);
        return root;
    }
}
